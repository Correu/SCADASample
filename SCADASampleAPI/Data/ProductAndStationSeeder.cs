using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Data;

/// <summary>
/// Seeds the Products catalog and Station groupings. Safe to run on every startup (idempotent).
/// </summary>
public static class ProductAndStationSeeder
{
    private static readonly Product[] KnownProducts =
    [
        new Product { Code = "DIESEL",   Name = "Diesel",        HexColor = "#f59e0b", ProductType = "Petroleum", Density = 850,  IsActive = true },
        new Product { Code = "GASOLINE", Name = "Gasoline",      HexColor = "#eab308", ProductType = "Petroleum", Density = 750,  IsActive = true },
        new Product { Code = "NATGAS",   Name = "Natural Gas",   HexColor = "#86efac", ProductType = "Gas",       Density = 0.8,  IsActive = true },
        new Product { Code = "OIL",      Name = "Crude Oil",     HexColor = "#92400e", ProductType = "Petroleum", Density = 900,  IsActive = true },
        new Product { Code = "WATER",    Name = "Water",         HexColor = "#38bdf8", ProductType = "Water",     Density = 1000, IsActive = true },
        new Product { Code = "WTR",      Name = "Process Water", HexColor = "#7dd3fc", ProductType = "Water",     Density = 1000, IsActive = true },
        new Product { Code = "MIX",      Name = "Mixed Blend",   HexColor = "#94a3b8", ProductType = "Blend",     Density = 820,  IsActive = true },
    ];

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedProductsAsync(db, cancellationToken);
        await SeedStationsAsync(db, cancellationToken);
    }

    private static async Task SeedProductsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var existingCodes = (await db.Products.Select(p => p.Code).ToListAsync(ct)).ToHashSet();

        var missing = KnownProducts.Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (missing.Count > 0)
        {
            db.Products.AddRange(missing);
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedStationsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var pipelines = await db.Pipelines
            .Include(p => p.ProcessLocations)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);

        foreach (var pipeline in pipelines)
        {
            var existingStations = await db.Stations
                .Where(s => s.PipelineId == pipeline.PipelineId)
                .ToListAsync(ct);

            if (existingStations.Count > 0)
                continue;

            var locationMap = pipeline.ProcessLocations
                .GroupBy(l => l.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            if (locationMap.Count == 0)
                continue;

            switch (pipeline.Code)
            {
                case "PL-N01":
                    await SeedNorthStationsAsync(db, pipeline.PipelineId, locationMap, ct);
                    break;
                case "PL-S02":
                    await SeedSouthStationsAsync(db, pipeline.PipelineId, locationMap, ct);
                    break;
                case "PL-E03":
                    await SeedEastStationsAsync(db, pipeline.PipelineId, locationMap, ct);
                    break;
                default:
                    await SeedGenericStationsAsync(db, pipeline.PipelineId, locationMap, ct);
                    break;
            }
        }
    }

    private static async Task SeedNorthStationsAsync(
        ApplicationDbContext db, int pipelineId,
        Dictionary<string, ProcessLocation> locs, CancellationToken ct)
    {
        var srcStation = new Station
        {
            PipelineId = pipelineId,
            Name = "North Source Station",
            Code = "N-STA-SRC",
            LayoutX = 50,
            LayoutY = 135
        };
        var midStation = new Station
        {
            PipelineId = pipelineId,
            Name = "North Midpoint Station",
            Code = "N-STA-MID",
            LayoutX = 165,
            LayoutY = 250
        };
        var delStation = new Station
        {
            PipelineId = pipelineId,
            Name = "North Delivery Station",
            Code = "N-STA-DEL",
            LayoutX = 285,
            LayoutY = 330
        };
        db.Stations.AddRange(srcStation, midStation, delStation);
        await db.SaveChangesAsync(ct);

        AssignStation(locs, srcStation.StationId, "N-SRC");
        AssignStation(locs, midStation.StationId, "N-CP1", "N-CP2", "N-SUB");
        AssignStation(locs, delStation.StationId, "N-CP3", "N-DIS");
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSouthStationsAsync(
        ApplicationDbContext db, int pipelineId,
        Dictionary<string, ProcessLocation> locs, CancellationToken ct)
    {
        var srcStation = new Station
        {
            PipelineId = pipelineId,
            Name = "South Source Station",
            Code = "S-STA-SRC",
            LayoutX = 50,
            LayoutY = 135
        };
        var ctrStation = new Station
        {
            PipelineId = pipelineId,
            Name = "South Central Station",
            Code = "S-STA-CTR",
            LayoutX = 565,
            LayoutY = 250
        };
        var delStation = new Station
        {
            PipelineId = pipelineId,
            Name = "South Delivery Station",
            Code = "S-STA-DEL",
            LayoutX = 285,
            LayoutY = 330
        };
        db.Stations.AddRange(srcStation, ctrStation, delStation);
        await db.SaveChangesAsync(ct);

        AssignStation(locs, srcStation.StationId, "S-SRC");
        AssignStation(locs, ctrStation.StationId, "S-CP1", "S-CP2", "S-SUB");
        AssignStation(locs, delStation.StationId, "S-CP3", "S-DIS");
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedEastStationsAsync(
        ApplicationDbContext db, int pipelineId,
        Dictionary<string, ProcessLocation> locs, CancellationToken ct)
    {
        var stgStation = new Station
        {
            PipelineId = pipelineId,
            Name = "East Storage Station",
            Code = "E-STA-STG",
            LayoutX = 450,
            LayoutY = 120
        };
        var proStation = new Station
        {
            PipelineId = pipelineId,
            Name = "East Processing Station",
            Code = "E-STA-PRO",
            LayoutX = 335,
            LayoutY = 255
        };
        var delStation = new Station
        {
            PipelineId = pipelineId,
            Name = "East Delivery Station",
            Code = "E-STA-DEL",
            LayoutX = 450,
            LayoutY = 380
        };
        db.Stations.AddRange(stgStation, proStation, delStation);
        await db.SaveChangesAsync(ct);

        AssignStation(locs, stgStation.StationId, "E-STO1", "E-STO2");
        AssignStation(locs, proStation.StationId, "E-CP1", "E-SUB");
        AssignStation(locs, delStation.StationId, "E-DIS");
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedGenericStationsAsync(
        ApplicationDbContext db, int pipelineId,
        Dictionary<string, ProcessLocation> locs, CancellationToken ct)
    {
        var station = new Station
        {
            PipelineId = pipelineId,
            Name = "Main Station",
            Code = "MAIN-STA",
            LayoutX = 450,
            LayoutY = 200
        };
        db.Stations.Add(station);
        await db.SaveChangesAsync(ct);

        AssignStation(locs, station.StationId, locs.Keys.ToArray());
        await db.SaveChangesAsync(ct);
    }

    private static void AssignStation(Dictionary<string, ProcessLocation> locs, int stationId, params string[] codes)
    {
        foreach (var code in codes)
        {
            if (locs.TryGetValue(code, out var loc))
                loc.StationId = stationId;
        }
    }
}
