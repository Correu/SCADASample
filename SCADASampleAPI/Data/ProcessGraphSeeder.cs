using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Data;

public static class ProcessGraphSeeder
{
    /// <summary>Seeds a small source → mix → discharge graph for each pipeline that has no process locations yet.</summary>
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var pipelines = await db.Pipelines
            .Where(p => !db.ProcessLocations.Any(l => l.PipelineId == p.PipelineId))
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

        if (pipelines.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (var p in pipelines)
        {
            var source = new ProcessLocation
            {
                PipelineId = p.PipelineId,
                Name = "Source tank",
                Code = "SRC",
                Kind = "Tank",
                Capacity = 100,
                CurrentVolume = 75,
                LayoutX = 80,
                LayoutY = 160,
                LastUpdatedUtc = now
            };
            var mix = new ProcessLocation
            {
                PipelineId = p.PipelineId,
                Name = "Mix tank",
                Code = "MIX",
                Kind = "Tank",
                Capacity = 120,
                CurrentVolume = 45,
                LayoutX = 320,
                LayoutY = 160,
                LastUpdatedUtc = now
            };
            var discharge = new ProcessLocation
            {
                PipelineId = p.PipelineId,
                Name = "Discharge tank",
                Code = "DIS",
                Kind = "Tank",
                Capacity = 100,
                CurrentVolume = 25,
                LayoutX = 560,
                LayoutY = 160,
                LastUpdatedUtc = now
            };

            db.ProcessLocations.AddRange(source, mix, discharge);
            await db.SaveChangesAsync(cancellationToken);

            source.Fluids.Add(new ProcessLocationFluid { FluidCode = "OIL", Volume = 75 });
            mix.Fluids.Add(new ProcessLocationFluid { FluidCode = "OIL", Volume = 25 });
            mix.Fluids.Add(new ProcessLocationFluid { FluidCode = "WTR", Volume = 20 });
            discharge.Fluids.Add(new ProcessLocationFluid { FluidCode = "OIL", Volume = 25 });

            db.ProcessTransfers.AddRange(
                new ProcessTransfer
                {
                    PipelineId = p.PipelineId,
                    FromLocationId = source.ProcessLocationId,
                    ToLocationId = mix.ProcessLocationId,
                    IsPumpRunning = false,
                    ValveOpen = true,
                    MaxFlowRate = 35,
                    CurrentFlowRate = 0,
                    LastUpdatedUtc = now,
                    Fluids = [new ProcessTransferFluid { FluidCode = "OIL", FlowRateFraction = 1.0, OutflowWeight = 1.0 }]
                },
                new ProcessTransfer
                {
                    PipelineId = p.PipelineId,
                    FromLocationId = mix.ProcessLocationId,
                    ToLocationId = discharge.ProcessLocationId,
                    IsPumpRunning = true,
                    ValveOpen = true,
                    MaxFlowRate = 28,
                    CurrentFlowRate = 0,
                    LastUpdatedUtc = now,
                    Fluids = [new ProcessTransferFluid { FluidCode = "OIL", FlowRateFraction = 1.0, OutflowWeight = 1.0 }]
                });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
