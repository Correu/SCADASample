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

                // Load transfers with their per-fluid entries and both endpoint location fluid buckets
                var transfers = await db.ProcessTransfers
                    .Include(t => t.Pipeline)
                    .Include(t => t.Fluids)
                    .Include(t => t.FromLocation!).ThenInclude(l => l.Fluids)
                    .Include(t => t.ToLocation!).ThenInclude(l => l.Fluids)
                    .Where(t => t.Pipeline!.IsActive)
                    .OrderBy(t => t.ProcessTransferId)
                    .ToListAsync(stoppingToken);

                var touchedLocations = new Dictionary<int, ProcessLocation>();

                // Compute allocation per (FromLocationId, FluidCode) group across all active transfer-fluid pairs.
                // Key: (transferFluidId) -> volume allocated this tick (m³)
                var tfAllocations = new Dictionary<int, double>();

                // Active legs: pump on, valve open, both endpoints present
                var activeTransfers = transfers
                    .Where(t => t.IsPumpRunning && t.ValveOpen && t.FromLocation != null && t.ToLocation != null)
                    .ToList();

                // Group all transfer-fluid pairs by (FromLocationId, FluidCode) to allocate source fairly
                var activePairs = activeTransfers
                    .SelectMany(t => t.Fluids.Select(f => (Transfer: t, TransferFluid: f)))
                    .ToList();

                foreach (var g in activePairs.GroupBy(p => (p.Transfer.FromLocationId, p.TransferFluid.FluidCode)))
                {
                    var list = g.ToList();
                    var src = list[0].Transfer.FromLocation!;
                    var srcFluid = GetOrCreateFluid(src, g.Key.FluidCode);
                    var available = srcFluid.Volume;
                    if (available <= 0)
                    {
                        foreach (var pair in list)
                            tfAllocations[pair.TransferFluid.ProcessTransferFluidId] = 0;
                        continue;
                    }

                    var weightSum = list.Sum(p => Math.Max(p.TransferFluid.OutflowWeight, 1e-9));
                    var desired = new List<(int Id, double Amount)>(list.Count);
                    foreach (var (t, tf) in list)
                    {
                        var dst = t.ToLocation!;
                        var fluidCap = t.MaxFlowRate * tf.FlowRateFraction * dtHours;
                        var toSpace = Math.Max(0, dst.Capacity - dst.CurrentVolume);
                        var w = Math.Max(tf.OutflowWeight, 1e-9);
                        var share = available * (w / weightSum);
                        desired.Add((tf.ProcessTransferFluidId, Math.Min(fluidCap, Math.Min(toSpace, share))));
                    }

                    var totalDesired = desired.Sum(d => d.Amount);
                    var scale = totalDesired > available && totalDesired > 0 ? available / totalDesired : 1.0;
                    foreach (var (id, amount) in desired)
                        tfAllocations[id] = amount * scale;
                }

                // Apply allocations: move fluid volumes and update flow rates
                foreach (var t in transfers)
                {
                    var src = t.FromLocation;
                    var dst = t.ToLocation;
                    if (src == null || dst == null) continue;

                    if (!t.IsPumpRunning || !t.ValveOpen)
                    {
                        foreach (var tf in t.Fluids)
                            tf.CurrentFlowRate = 0;
                        t.CurrentFlowRate = 0;
                        t.LastUpdatedUtc = now;
                        continue;
                    }

                    var totalDv = 0.0;
                    foreach (var tf in t.Fluids)
                    {
                        if (!tfAllocations.TryGetValue(tf.ProcessTransferFluidId, out var dv) || dv <= 0)
                        {
                            tf.CurrentFlowRate = 0;
                            continue;
                        }

                        var srcFluid = GetOrCreateFluid(src, tf.FluidCode);
                        var dstFluid = GetOrCreateFluid(dst, tf.FluidCode);
                        srcFluid.Volume = Math.Max(0, srcFluid.Volume - dv);
                        dstFluid.Volume += dv;

                        tf.CurrentFlowRate = dtHours > 0 ? dv / dtHours : 0;
                        totalDv += dv;
                    }

                    if (totalDv > 0)
                    {
                        RecomputeLocationVolume(src);
                        RecomputeLocationVolume(dst);
                        src.LastUpdatedUtc = now;
                        dst.LastUpdatedUtc = now;
                        touchedLocations[src.ProcessLocationId] = src;
                        touchedLocations[dst.ProcessLocationId] = dst;
                    }

                    t.CurrentFlowRate = dtHours > 0 ? totalDv / dtHours : 0;
                    t.LastUpdatedUtc = now;
                }

                // Push location updates for touched locations
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

                // Push transfer updates — now includes per-fluid flow details
                foreach (var t in transfers)
                {
                    var dto = new TransferUpdateDto
                    {
                        PipelineId = t.PipelineId,
                        ProcessTransferId = t.ProcessTransferId,
                        CurrentFlowRate = t.CurrentFlowRate,
                        IsPumpRunning = t.IsPumpRunning,
                        ValveOpen = t.ValveOpen,
                        LastUpdatedUtc = t.LastUpdatedUtc,
                        Fluids = t.Fluids
                            .OrderBy(f => f.FluidCode)
                            .Select(f => new TransferFluidDto
                            {
                                FluidCode = f.FluidCode,
                                FlowRateFraction = f.FlowRateFraction,
                                CurrentFlowRate = f.CurrentFlowRate
                            })
                            .ToList()
                    };
                    await hubContext.Clients.All.SendAsync("TransferUpdate", dto, stoppingToken);
                }

                // Tag random-walk and alarm evaluation
                var tags = await db.Tags
                    .Include(t => t.Pipeline)
                    .Where(t => t.Pipeline!.IsActive)
                    .ToListAsync(stoppingToken);

                var alarms = await db.Alarms
                    .Where(a => a.IsEnabled)
                    .ToListAsync(stoppingToken);

                foreach (var tag in tags)
                {
                    var range = tag.MaxValue - tag.MinValue;
                    if (range <= 0) continue;

                    var step = range * _options.StepFraction * (Random.Shared.NextDouble() * 2 - 1);
                    var next = tag.CurrentValue + step;
                    if (next < tag.MinValue)
                        next = tag.MinValue + Random.Shared.NextDouble() * range * 0.1;
                    if (next > tag.MaxValue)
                        next = tag.MaxValue - Random.Shared.NextDouble() * range * 0.1;

                    tag.CurrentValue = Math.Round(next, 3);
                    tag.LastUpdatedUtc = DateTimeOffset.UtcNow;

                    var tagDto = new TagValueUpdateDto
                    {
                        PipelineId = tag.PipelineId,
                        TagId = tag.TagId,
                        TagName = tag.Name,
                        Value = tag.CurrentValue,
                        Unit = tag.Unit,
                        TimestampUtc = tag.LastUpdatedUtc
                    };
                    await hubContext.Clients.All.SendAsync("TagUpdate", tagDto, stoppingToken);

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
        if (f != null) return f;
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
        loc.CurrentVolume = Math.Clamp(loc.Fluids.Sum(x => x.Volume), 0, loc.Capacity);
    }
}
