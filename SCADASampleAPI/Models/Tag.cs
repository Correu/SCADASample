using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Process tag / point. Mapped to <c>ScadaTags</c> to avoid clashing with an existing SQL <c>Tags</c> table.</summary>
[Table("ScadaTags")]
public class Tag
{
    public int TagId { get; set; }

    public int PipelineId { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = "";

    [MaxLength(32)]
    public string Unit { get; set; } = "";

    public double MinValue { get; set; }

    public double MaxValue { get; set; }

    public double CurrentValue { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; }

    [ForeignKey(nameof(PipelineId))]
    public Pipeline? Pipeline { get; set; }

    public ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
}
