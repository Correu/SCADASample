using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Data;

/// <summary>
/// Replaces legacy SRC/MIX/DIS graphs with richer checkpoint/substation topologies per pipeline code.
/// Supports multi-fluid transfer legs where a single pipe segment simultaneously carries several products.
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

            await db.ProcessTransfers
                .Where(t => t.PipelineId == pipeline.PipelineId)
                .ExecuteDeleteAsync(cancellationToken);

            await db.ProcessLocations
                .Where(l => locationIds.Contains(l.ProcessLocationId))
                .ExecuteDeleteAsync(cancellationToken);

            // Clear old stations so ProductAndStationSeeder can recreate them with correct assignments
            await db.Stations
                .Where(s => s.PipelineId == pipeline.PipelineId)
                .ExecuteDeleteAsync(cancellationToken);

            // ExecuteDeleteAsync does not update the change tracker; clear so later seeders
            // do not see deleted location entities alongside newly inserted ones.
            db.ChangeTracker.Clear();

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

    // PL-N01: North Transfer — primarily DIESEL with a NATGAS injection line
    private static async Task SeedNorthAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("N-SRC",  "North Source",     450, 50,  150, 120, new[] { ("DIESEL", 95.0),  ("NATGAS", 18.0) }),
            ("N-CP1",  "Checkpoint 1",     450, 130, 120, 70,  new[] { ("DIESEL", 55.0),  ("NATGAS", 12.0) }),
            ("N-CP2",  "Checkpoint 2",     450, 210, 120, 85,  new[] { ("DIESEL", 70.0),  ("NATGAS", 8.0)  }),
            ("N-SUB",  "Substation",       220, 290, 80,  35,  new[] { ("DIESEL", 20.0) }),
            ("N-CP3",  "Mainline CP3",     450, 290, 120, 60,  new[] { ("DIESEL", 45.0),  ("NATGAS", 5.0)  }),
            ("N-DIS",  "North Discharge",  450, 370, 150, 40,  new[] { ("DIESEL", 35.0),  ("NATGAS", 3.0)  }));

        // Mainline carries DIESEL + NATGAS together (multi-fluid legs); substation branch is DIESEL only
        AddTransfers(db, pipelineId, now, locs,
            ("N-SRC", "N-CP1", multiFluid: new[] { ("DIESEL", 0.80, 1.0), ("NATGAS", 0.20, 1.0) }, maxFlow: 40, pumpOn: true),
            ("N-CP1", "N-CP2", multiFluid: new[] { ("DIESEL", 0.80, 1.0), ("NATGAS", 0.20, 1.0) }, maxFlow: 38, pumpOn: true),
            ("N-CP2", "N-SUB", multiFluid: new[] { ("DIESEL", 1.0,  0.3) },                         maxFlow: 32, pumpOn: true),
            ("N-CP2", "N-CP3", multiFluid: new[] { ("DIESEL", 0.80, 0.7), ("NATGAS", 0.20, 0.7) }, maxFlow: 36, pumpOn: true),
            ("N-SUB", "N-CP3", multiFluid: new[] { ("DIESEL", 1.0,  1.0) },                         maxFlow: 28, pumpOn: false),
            ("N-CP3", "N-DIS", multiFluid: new[] { ("DIESEL", 0.80, 1.0), ("NATGAS", 0.20, 1.0) }, maxFlow: 34, pumpOn: true));

        await db.SaveChangesAsync(ct);
    }

    // PL-S02: South Blending — GASOLINE with WATER for injection/flushing; substation leg gets both
    private static async Task SeedSouthAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("S-SRC",  "South Source",     450, 50,  140, 110, new[] { ("GASOLINE", 90.0), ("WATER", 15.0) }),
            ("S-CP1",  "Checkpoint 1",     450, 130, 110, 65,  new[] { ("GASOLINE", 50.0), ("WATER", 10.0) }),
            ("S-CP2",  "Checkpoint 2",     450, 210, 110, 75,  new[] { ("GASOLINE", 60.0), ("WATER", 8.0)  }),
            ("S-SUB",  "Substation",       680, 290, 90,  40,  new[] { ("GASOLINE", 25.0), ("WATER", 6.0)  }),
            ("S-CP3",  "Mainline CP3",     450, 290, 110, 55,  new[] { ("GASOLINE", 40.0) }),
            ("S-DIS",  "South Discharge",  450, 370, 140, 35,  new[] { ("GASOLINE", 30.0), ("WATER", 5.0)  }));

        AddTransfers(db, pipelineId, now, locs,
            ("S-SRC", "S-CP1", multiFluid: new[] { ("GASOLINE", 0.85, 1.0), ("WATER", 0.15, 1.0) }, maxFlow: 42, pumpOn: true),
            ("S-CP1", "S-CP2", multiFluid: new[] { ("GASOLINE", 0.85, 1.0), ("WATER", 0.15, 1.0) }, maxFlow: 40, pumpOn: true),
            ("S-CP2", "S-SUB", multiFluid: new[] { ("GASOLINE", 0.80, 0.35), ("WATER", 0.20, 0.35) }, maxFlow: 30, pumpOn: true),
            ("S-CP2", "S-CP3", multiFluid: new[] { ("GASOLINE", 1.0,  0.65) },                         maxFlow: 35, pumpOn: true),
            ("S-SUB", "S-DIS", multiFluid: new[] { ("GASOLINE", 0.80, 1.0), ("WATER", 0.20, 1.0) }, maxFlow: 26, pumpOn: false),
            ("S-CP3", "S-DIS", multiFluid: new[] { ("GASOLINE", 1.0,  1.0) },                         maxFlow: 32, pumpOn: true));

        await db.SaveChangesAsync(ct);
    }

    // PL-E03: East Storage — OIL + WATER co-transport; trunk checkpoint carries both products simultaneously
    private static async Task SeedEastAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("E-STO1", "Storage Tank A",   180, 120, 120, 80,  new[] { ("OIL", 55.0),   ("WATER", 10.0) }),
            ("E-STO2", "Storage Tank B",   720, 120, 120, 75,  new[] { ("WATER", 60.0), ("OIL", 5.0)   }),
            ("E-CP1",  "Trunk Checkpoint", 450, 200, 130, 90,  new[] { ("OIL", 35.0),   ("WATER", 40.0) }),
            ("E-SUB",  "Substation",       220, 310, 90,  45,  new[] { ("OIL", 20.0) }),
            ("E-DIS",  "East Discharge",   450, 380, 150, 50,  new[] { ("OIL", 15.0),   ("WATER", 25.0) }));

        // E-STO1 feeds OIL into the trunk; E-STO2 feeds WATER.
        // The E-CP1 → E-DIS leg is a true multi-fluid segment: both OIL and WATER flow simultaneously.
        AddTransfers(db, pipelineId, now, locs,
            ("E-STO1", "E-CP1", multiFluid: new[] { ("OIL",   1.0, 1.0) },                        maxFlow: 30, pumpOn: true),
            ("E-STO2", "E-CP1", multiFluid: new[] { ("WATER", 1.0, 1.0) },                        maxFlow: 28, pumpOn: true),
            ("E-CP1",  "E-SUB", multiFluid: new[] { ("OIL",   1.0, 0.4) },                        maxFlow: 24, pumpOn: true),
            ("E-CP1",  "E-DIS", multiFluid: new[] { ("OIL", 0.55, 0.6), ("WATER", 0.45, 1.0) }, maxFlow: 32, pumpOn: true),
            ("E-SUB",  "E-DIS", multiFluid: new[] { ("OIL",   1.0, 1.0) },                        maxFlow: 22, pumpOn: false));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedGenericNetworkAsync(ApplicationDbContext db, int pipelineId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var locs = await AddLocationsAsync(db, pipelineId, now, ct,
            ("SRC", "Source",     200, 200, 100, 70, new[] { ("MIX", 65.0) }),
            ("CP1", "Checkpoint", 450, 200, 110, 50, new[] { ("MIX", 40.0) }),
            ("SUB", "Substation", 200, 320, 80,  30, new[] { ("MIX", 15.0) }),
            ("DIS", "Discharge",  700, 200, 100, 35, new[] { ("MIX", 30.0) }));

        AddTransfers(db, pipelineId, now, locs,
            ("SRC", "CP1", multiFluid: new[] { ("MIX", 1.0, 1.0) }, maxFlow: 35, pumpOn: true),
            ("CP1", "SUB", multiFluid: new[] { ("MIX", 1.0, 0.3) }, maxFlow: 28, pumpOn: true),
            ("CP1", "DIS", multiFluid: new[] { ("MIX", 1.0, 0.7) }, maxFlow: 30, pumpOn: true));

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

    /// <param name="multiFluid">Tuples of (FluidCode, FlowRateFraction, OutflowWeight). Fractions should sum to 1.0 per leg.</param>
    private static void AddTransfers(
        ApplicationDbContext db,
        int pipelineId,
        DateTimeOffset now,
        Dictionary<string, ProcessLocation> locs,
        params (string From, string To, (string FluidCode, double Fraction, double Weight)[] multiFluid, double maxFlow, bool pumpOn)[] specs)
    {
        foreach (var s in specs)
        {
            var transfer = new ProcessTransfer
            {
                PipelineId = pipelineId,
                FromLocationId = locs[s.From].ProcessLocationId,
                ToLocationId = locs[s.To].ProcessLocationId,
                MaxFlowRate = s.maxFlow,
                IsPumpRunning = s.pumpOn,
                ValveOpen = true,
                CurrentFlowRate = 0,
                LastUpdatedUtc = now
            };

            foreach (var (fluidCode, fraction, weight) in s.multiFluid)
            {
                transfer.Fluids.Add(new ProcessTransferFluid
                {
                    FluidCode = fluidCode,
                    FlowRateFraction = fraction,
                    OutflowWeight = weight,
                    CurrentFlowRate = 0
                });
            }

            db.ProcessTransfers.Add(transfer);
        }
    }
}
