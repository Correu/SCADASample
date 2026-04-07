using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Tank/silo-style node in a pipeline process graph. Mapped to <c>ScadaProcessLocations</c>.</summary>
[Table("ScadaProcessLocations")]
public class ProcessLocation
{
    public int ProcessLocationId { get; set; }

    public int PipelineId { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = "";

    [MaxLength(32)]
    public string Code { get; set; } = "";

    /// <summary>Optional kind label (e.g. Tank, Silo).</summary>
    [MaxLength(64)]
    public string? Kind { get; set; }

    /// <summary>Maximum volume (m³).</summary>
    public double Capacity { get; set; }

    /// <summary>Current volume (m³).</summary>
    public double CurrentVolume { get; set; }

    public double LayoutX { get; set; }

    public double LayoutY { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; }

    [ForeignKey(nameof(PipelineId))]
    public Pipeline? Pipeline { get; set; }
}
