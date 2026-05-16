using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Data;

/// <summary>
/// Replaces legacy SRC/MIX/DIS graphs with richer checkpoint/substation topologies per pipeline code.
/// Gated by configuration <c>Scada:ReseedProcessGraph</c> (default false).
/// </summary>
public static class ProcessNetworkV2Seeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var forceReseed = configuration.GetValue("Scada:ReseedProcessGraph", false);
        var pipelines = await db.Pipelines.OrderBy(p => p.Code).ToListAsync(cancellationToken);

        foreach (var pipeline in pipelines)
        {
            var locations = await db.ProcessLocations
                .Where(l => l.PipelineId == pipeline.PipelineId)
                .ToListAsync(cancellationToken);

            if (locations.Count == 0)
                continue;

            var isLegacy = locations.Count == 3
                && locations.Any(l => l.Code == "SRC")
                && locations.Any(l => l.Code == "MIX")
                && locations.Any(l => l.Code == "DIS");

            if (!isLegacy && !forceReseed)
                continue;

            var locationIds = locations.Select(l => l.ProcessLocationId).ToList();

            // Bulk deletes bypass the change tracker; do not mix with RemoveRange/SaveChanges on the same entities.
            await db.ProcessTransfers
                .Where(t => t.PipelineId == pipeline.PipelineId)
                .ExecuteDeleteAsync(cancellationToken);

            await db.ProcessLocations
                .Where(l => locationIds.Contains(l.ProcessLocationId))
                .ExecuteDeleteAsync(cancellationToken);

            switch (pipeline.Code)
            {
                case "PL-N01":
                    await SeedNorthAsync(db, pipeline.PipelineId, cancellationToken);
                    break;
                case "PL-S02":
                    await SeedSouthAsync(db, pipeline.PipelineId, cancellationToken);
                    break;
                case "PL-E03":
                    await SeedEastAsync(db, pipeline.PipelineId, cancellationToken);
                    break;
                default:
                    await SeedGenericNetworkAsync(db, pipeline.PipelineId, cancellationToken);
                    break;
            }
        }
    }

    private static async Task SeedNorthAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("N-SRC", "North source", 450, 50, 150, 120, new[] { ("DIESEL", 95.0) }),
            ("N-CP1", "Checkpoint 1", 450, 130, 120, 70, new[] { ("DIESEL", 55.0) }),
            ("N-CP2", "Checkpoint 2", 450, 210, 120, 85, new[] { ("DIESEL", 70.0) }),
            ("N-SUB", "Substation", 220, 290, 80, 35, new[] { ("DIESEL", 20.0) }),
            ("N-CP3", "Mainline CP3", 450, 290, 120, 60, new[] { ("DIESEL", 45.0) }),
            ("N-DIS", "Discharge", 450, 370, 150, 40, new[] { ("DIESEL", 35.0) }));

        AddTransfers(db, pipelineId, now, locs,
            ("N-SRC", "N-CP1", "DIESEL", 1, 40, true),
            ("N-CP1", "N-CP2", "DIESEL", 1, 38, true),
            ("N-CP2", "N-SUB", "DIESEL", 0.3, 32, true),
            ("N-CP2", "N-CP3", "DIESEL", 0.7, 36, true),
            ("N-SUB", "N-CP3", "DIESEL", 1, 28, false),
            ("N-CP3", "N-DIS", "DIESEL", 1, 34, true));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSouthAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("S-SRC", "South source", 450, 50, 140, 110, new[] { ("WATER", 90.0) }),
            ("S-CP1", "Checkpoint 1", 450, 130, 110, 65, new[] { ("WATER", 50.0) }),
            ("S-CP2", "Checkpoint 2", 450, 210, 110, 75, new[] { ("WATER", 60.0) }),
            ("S-SUB", "Substation", 680, 290, 90, 40, new[] { ("WATER", 25.0) }),
            ("S-CP3", "Mainline CP3", 450, 290, 110, 55, new[] { ("WATER", 40.0) }),
            ("S-DIS", "Discharge", 450, 370, 140, 35, new[] { ("WATER", 30.0) }));

        AddTransfers(db, pipelineId, now, locs,
            ("S-SRC", "S-CP1", "WATER", 1, 42, true),
            ("S-CP1", "S-CP2", "WATER", 1, 40, true),
            ("S-CP2", "S-SUB", "WATER", 0.35, 30, true),
            ("S-CP2", "S-CP3", "WATER", 0.65, 35, true),
            ("S-SUB", "S-DIS", "WATER", 1, 26, false),
            ("S-CP3", "S-DIS", "WATER", 1, 32, true));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedEastAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("E-STO1", "Storage A", 180, 120, 120, 80, new[] { ("OIL", 55.0), ("WTR", 10.0) }),
            ("E-STO2", "Storage B", 720, 120, 120, 75, new[] { ("WTR", 60.0) }),
            ("E-CP1", "Trunk checkpoint", 450, 200, 130, 90, new[] { ("OIL", 35.0), ("WTR", 40.0) }),
            ("E-SUB", "Substation", 220, 310, 90, 45, new[] { ("OIL", 20.0) }),
            ("E-DIS", "Discharge", 450, 380, 150, 50, new[] { ("OIL", 15.0), ("WTR", 25.0) }));

        AddTransfers(db, pipelineId, now, locs,
            ("E-STO1", "E-CP1", "OIL", 1, 30, true),
            ("E-STO2", "E-CP1", "WTR", 1, 28, true),
            ("E-CP1", "E-SUB", "OIL", 0.4, 24, true),
            ("E-CP1", "E-DIS", "WTR", 0.6, 32, true),
            ("E-SUB", "E-DIS", "OIL", 1, 22, false));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedGenericNetworkAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("SRC", "Source", 200, 200, 100, 70, new[] { ("MIX", 65.0) }),
            ("CP1", "Checkpoint", 450, 200, 110, 50, new[] { ("MIX", 40.0) }),
            ("SUB", "Substation", 200, 320, 80, 30, new[] { ("MIX", 15.0) }),
            ("DIS", "Discharge", 700, 200, 100, 35, new[] { ("MIX", 30.0) }));

        AddTransfers(db, pipelineId, now, locs,
            ("SRC", "CP1", "MIX", 1, 35, true),
            ("CP1", "SUB", "MIX", 0.3, 28, true),
            ("CP1", "DIS", "MIX", 0.7, 30, true));

        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<string, ProcessLocation>> AddLocationsAsync(
        ApplicationDbContext db,
        int pipelineId,
        DateTimeOffset now,
        CancellationToken ct,
        params (string Code, string Name, double X, double Y, double Capacity, double CurrentVolume, (string Fluid, double Vol)[] Fluids)[] specs)
    {
        var map = new Dictionary<string, ProcessLocation>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in specs)
        {
            var loc = new ProcessLocation
            {
                PipelineId = pipelineId,
                Code = s.Code,
                Name = s.Name,
                Kind = "Tank",
                LayoutX = s.X,
                LayoutY = s.Y,
                Capacity = s.Capacity,
                CurrentVolume = s.CurrentVolume,
                LastUpdatedUtc = now
            };
            foreach (var (fluid, vol) in s.Fluids)
                loc.Fluids.Add(new ProcessLocationFluid { FluidCode = fluid, Volume = vol });
            db.ProcessLocations.Add(loc);
            map[s.Code] = loc;
        }

        await db.SaveChangesAsync(ct);
        return map;
    }

    private static void AddTransfers(
        ApplicationDbContext db,
        int pipelineId,
        DateTimeOffset now,
        Dictionary<string, ProcessLocation> locs,
        params (string From, string To, string Fluid, double Weight, double MaxFlow, bool PumpOn)[] specs)
    {
        foreach (var s in specs)
        {
            db.ProcessTransfers.Add(new ProcessTransfer
            {
                PipelineId = pipelineId,
                FromLocationId = locs[s.From].ProcessLocationId,
                ToLocationId = locs[s.To].ProcessLocationId,
                FluidCode = s.Fluid,
                OutflowWeight = s.Weight,
                MaxFlowRate = s.MaxFlow,
                IsPumpRunning = s.PumpOn,
                ValveOpen = true,
                CurrentFlowRate = 0,
                LastUpdatedUtc = now
            });
        }
    }
}
