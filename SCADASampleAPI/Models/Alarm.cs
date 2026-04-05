using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Mapped to <c>ScadaAlarms</c> to avoid colliding with an existing SQL <c>Alarms</c> table.</summary>
[Table("ScadaAlarms")]
public class Alarm
{
    [Key]
    public int AlarmId { get; set; }

    public int PipelineId { get; set; }

    public int TagId { get; set; }

    [MaxLength(64)]
    public string AlarmType { get; set; } = "";

    public float SetPoint { get; set; }

    public int Severity { get; set; }

    [MaxLength(512)]
    public string Message { get; set; } = "";

    public bool IsEnabled { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset? RaisedAt { get; set; }

    public DateTimeOffset? AcknowledgedAt { get; set; }

    [MaxLength(450)]
    public string? AcknowledgedByUserId { get; set; }

    [ForeignKey(nameof(PipelineId))]
    public Pipeline? Pipeline { get; set; }

    [ForeignKey(nameof(TagId))]
    public Tag? Tag { get; set; }
}
