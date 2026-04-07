using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Directed fluid transfer (pump/line) between two locations. Mapped to <c>ScadaProcessTransfers</c>.</summary>
[Table("ScadaProcessTransfers")]
public class ProcessTransfer
{
    public int ProcessTransferId { get; set; }

    public int PipelineId { get; set; }

    public int FromLocationId { get; set; }

    public int ToLocationId { get; set; }

    public bool IsPumpRunning { get; set; }

    public bool ValveOpen { get; set; } = true;

    /// <summary>Maximum flow rate (m³/h).</summary>
    public double MaxFlowRate { get; set; }

    /// <summary>Instantaneous flow rate (m³/h) from last simulation step.</summary>
    public double CurrentFlowRate { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; }

    [ForeignKey(nameof(PipelineId))]
    public Pipeline? Pipeline { get; set; }

    [ForeignKey(nameof(FromLocationId))]
    public ProcessLocation? FromLocation { get; set; }

    [ForeignKey(nameof(ToLocationId))]
    public ProcessLocation? ToLocation { get; set; }
}
