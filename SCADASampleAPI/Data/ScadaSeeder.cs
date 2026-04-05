using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Data;

public static class ScadaSeeder
{
    public static async Task SeedScadaAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Pipelines.AnyAsync(cancellationToken))
            return;

        var pipelines = new[]
        {
            new Pipeline { Name = "North Transfer", Code = "PL-N01", IsActive = true },
            new Pipeline { Name = "South Blending", Code = "PL-S02", IsActive = true },
            new Pipeline { Name = "East Storage", Code = "PL-E03", IsActive = true }
        };

        db.Pipelines.AddRange(pipelines);
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Pipelines.OrderBy(p => p.Code).ToListAsync(cancellationToken);

        foreach (var p in reloaded)
        {
            var mid = 50.0;
            var tags = new List<Tag>
            {
                new()
                {
                    PipelineId = p.PipelineId,
                    Name = "Inlet Temperature",
                    Unit = "°C",
                    MinValue = 20,
                    MaxValue = 90,
                    CurrentValue = mid,
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                },
                new()
                {
                    PipelineId = p.PipelineId,
                    Name = "Discharge Pressure",
                    Unit = "bar",
                    MinValue = 0.5,
                    MaxValue = 8,
                    CurrentValue = 3.5,
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                },
                new()
                {
                    PipelineId = p.PipelineId,
                    Name = "Flow Rate",
                    Unit = "m³/h",
                    MinValue = 10,
                    MaxValue = 120,
                    CurrentValue = 65,
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                },
                new()
                {
                    PipelineId = p.PipelineId,
                    Name = "Tank Level",
                    Unit = "%",
                    MinValue = 5,
                    MaxValue = 95,
                    CurrentValue = 55,
                    LastUpdatedUtc = DateTimeOffset.UtcNow
                }
            };

            db.Tags.AddRange(tags);
        }

        await db.SaveChangesAsync(cancellationToken);

        var allTags = await db.Tags.Include(t => t.Pipeline).ToListAsync(cancellationToken);
        foreach (var tag in allTags.Where(t => t.Name == "Inlet Temperature"))
        {
            db.Alarms.Add(new Alarm
            {
                PipelineId = tag.PipelineId,
                TagId = tag.TagId,
                AlarmType = "HI",
                SetPoint = 75f,
                Severity = 2,
                Message = $"{tag.Pipeline?.Name}: inlet temperature high",
                IsEnabled = true,
                IsActive = false
            });
        }

        foreach (var tag in allTags.Where(t => t.Name == "Tank Level"))
        {
            db.Alarms.Add(new Alarm
            {
                PipelineId = tag.PipelineId,
                TagId = tag.TagId,
                AlarmType = "LO",
                SetPoint = 15f,
                Severity = 1,
                Message = $"{tag.Pipeline?.Name}: tank level low",
                IsEnabled = true,
                IsActive = false
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
