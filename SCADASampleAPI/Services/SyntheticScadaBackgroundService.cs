using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SCADASampleAPI.Contracts;
using SCADASampleAPI.Data;
using SCADASampleAPI.Hubs;
using SCADASampleAPI.Models;
using SCADASampleAPI.Options;

namespace SCADASampleAPI.Services;

public class SyntheticScadaBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyntheticScadaBackgroundService> _logger;
    private readonly SyntheticScadaOptions _options;

    public SyntheticScadaBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SyntheticScadaOptions> options,
        ILogger<SyntheticScadaBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ProcessHub>>();

                var now = DateTimeOffset.UtcNow;
                var dtHours = Math.Max(1, _options.UpdateIntervalSeconds) / 3600.0;

                var transfers = await db.ProcessTransfers
                    .Include(t => t.Pipeline)
                    .Include(t => t.FromLocation!).ThenInclude(l => l.Fluids)
                    .Include(t => t.ToLocation!).ThenInclude(l => l.Fluids)
                    .Where(t => t.Pipeline!.IsActive)
                    .OrderBy(t => t.ProcessTransferId)
                    .ToListAsync(stoppingToken);

                var touchedLocations = new Dictionary<int, ProcessLocation>();
                var allocations = transfers.ToDictionary(t => t.ProcessTransferId, _ => 0.0);

                var activeTransfers = transfers
                    .Where(t => t.IsPumpRunning && t.ValveOpen && t.FromLocation != null && t.ToLocation != null)
                    .ToList();

                foreach (var g in activeTransfers.GroupBy(t => (t.FromLocationId, t.FluidCode)))
                {
                    var list = g.ToList();
                    var src = list[0].FromLocation!;
                    var srcFluid = GetOrCreateFluid(src, g.Key.Item2);
                    var fluidAvailable = srcFluid.Volume;
                    if (fluidAvailable <= 0)
                        continue;

                    var weightSum = list.Sum(t => Math.Max(t.OutflowWeight, 1e-9));
                    var desired = new List<double>(list.Count);
                    foreach (var t in list)
                    {
                        var dst = t.ToLocation!;
                        var cap = t.MaxFlowRate * dtHours;
                        var toSpace = Math.Max(0, dst.Capacity - dst.CurrentVolume);
                        var w = Math.Max(t.OutflowWeight, 1e-9);
                        var fluidShare = fluidAvailable * (w / weightSum);
                        desired.Add(Math.Min(cap, Math.Min(toSpace, fluidShare)));
                    }

                    var totalDesired = desired.Sum();
                    var scale = totalDesired > fluidAvailable && totalDesired > 0 ? fluidAvailable / totalDesired : 1.0;
                    for (var i = 0; i < list.Count; i++)
                        allocations[list[i].ProcessTransferId] = desired[i] * scale;
                }

                foreach (var t in transfers)
                {
                    var src = t.FromLocation;
                    var dst = t.ToLocation;
                    if (src == null || dst == null)
                        continue;

                    if (!t.IsPumpRunning || !t.ValveOpen)
                    {
                        t.CurrentFlowRate = 0;
                        t.LastUpdatedUtc = now;
                        continue;
                    }

                    var dv = allocations[t.ProcessTransferId];
                    if (dv <= 0)
                    {
                        t.CurrentFlowRate = 0;
                        t.LastUpdatedUtc = now;
                        continue;
                    }

                    var srcFluid = GetOrCreateFluid(src, t.FluidCode);
                    var dstFluid = GetOrCreateFluid(dst, t.FluidCode);
                    srcFluid.Volume -= dv;
                    dstFluid.Volume += dv;
                    if (srcFluid.Volume < 0)
                        srcFluid.Volume = 0;

                    RecomputeLocationVolume(src);
                    RecomputeLocationVolume(dst);

                    t.CurrentFlowRate = dtHours > 0 ? dv / dtHours : 0;
                    t.LastUpdatedUtc = now;
                    src.LastUpdatedUtc = now;
                    dst.LastUpdatedUtc = now;
                    touchedLocations[src.ProcessLocationId] = src;
                    touchedLocations[dst.ProcessLocationId] = dst;
                }

                var tags = await db.Tags
                    .Include(t => t.Pipeline)
                    .Where(t => t.Pipeline!.IsActive)
                    .ToListAsync(stoppingToken);

                var alarms = await db.Alarms
                    .Where(a => a.IsEnabled)
                    .ToListAsync(stoppingToken);

                foreach (var loc in touchedLocations.Values)
                {
                    var dto = new LocationUpdateDto
                    {
                        PipelineId = loc.PipelineId,
                        ProcessLocationId = loc.ProcessLocationId,
                        CurrentVolume = loc.CurrentVolume,
                        Capacity = loc.Capacity,
                        LastUpdatedUtc = loc.LastUpdatedUtc,
                        Fluids = loc.Fluids
                            .OrderBy(f => f.FluidCode)
                            .Select(f => new ProcessLocationFluidDto { FluidCode = f.FluidCode, Volume = f.Volume })
                            .ToList()
                    };
                    await hubContext.Clients.All.SendAsync("LocationUpdate", dto, stoppingToken);
                }

                foreach (var t in transfers)
                {
                    var dto = new TransferUpdateDto
                    {
                        PipelineId = t.PipelineId,
                        ProcessTransferId = t.ProcessTransferId,
                        FluidCode = t.FluidCode,
                        CurrentFlowRate = t.CurrentFlowRate,
                        IsPumpRunning = t.IsPumpRunning,
                        ValveOpen = t.ValveOpen,
                        LastUpdatedUtc = t.LastUpdatedUtc
                    };
                    await hubContext.Clients.All.SendAsync("TransferUpdate", dto, stoppingToken);
                }

                foreach (var tag in tags)
                {
                    var range = tag.MaxValue - tag.MinValue;
                    if (range <= 0)
                        continue;

                    var step = range * _options.StepFraction * (Random.Shared.NextDouble() * 2 - 1);
                    var next = tag.CurrentValue + step;
                    if (next < tag.MinValue)
                        next = tag.MinValue + Random.Shared.NextDouble() * range * 0.1;
                    if (next > tag.MaxValue)
                        next = tag.MaxValue - Random.Shared.NextDouble() * range * 0.1;

                    tag.CurrentValue = Math.Round(next, 3);
                    tag.LastUpdatedUtc = DateTimeOffset.UtcNow;

                    var dto = new TagValueUpdateDto
                    {
                        PipelineId = tag.PipelineId,
                        TagId = tag.TagId,
                        TagName = tag.Name,
                        Value = tag.CurrentValue,
                        Unit = tag.Unit,
                        TimestampUtc = tag.LastUpdatedUtc
                    };

                    await hubContext.Clients.All.SendAsync("TagUpdate", dto, stoppingToken);

                    foreach (var alarm in alarms.Where(a => a.TagId == tag.TagId && a.PipelineId == tag.PipelineId))
                    {
                        var active = alarm.AlarmType.ToUpperInvariant() switch
                        {
                            "HI" or "HIGH" => tag.CurrentValue > alarm.SetPoint,
                            "LO" or "LOW" => tag.CurrentValue < alarm.SetPoint,
                            _ => false
                        };

                        if (active && !alarm.IsActive)
                        {
                            alarm.IsActive = true;
                            alarm.RaisedAt = DateTimeOffset.UtcNow;
                            alarm.AcknowledgedAt = null;
                            alarm.AcknowledgedByUserId = null;
                        }
                        else if (!active && alarm.IsActive)
                        {
                            alarm.IsActive = false;
                            alarm.RaisedAt = null;
                        }
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Synthetic SCADA tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.UpdateIntervalSeconds)), stoppingToken);
        }
    }

    private static ProcessLocationFluid GetOrCreateFluid(ProcessLocation loc, string fluidCode)
    {
        var f = loc.Fluids.FirstOrDefault(x => x.FluidCode == fluidCode);
        if (f != null)
            return f;
        f = new ProcessLocationFluid
        {
            ProcessLocationId = loc.ProcessLocationId,
            FluidCode = fluidCode,
            Volume = 0
        };
        loc.Fluids.Add(f);
        return f;
    }

    private static void RecomputeLocationVolume(ProcessLocation loc)
    {
        loc.CurrentVolume = loc.Fluids.Sum(x => x.Volume);
        loc.CurrentVolume = Math.Clamp(loc.CurrentVolume, 0, loc.Capacity);
    }
}
