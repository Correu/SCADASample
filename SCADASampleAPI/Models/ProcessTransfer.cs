using System.ComponentModel.DataAnnotations;
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

    /// <summary>Maximum combined flow rate across all fluids on this leg (m³/h).</summary>
    public double MaxFlowRate { get; set; }

    /// <summary>Combined instantaneous flow rate from last simulation tick (m³/h).</summary>
    public double CurrentFlowRate { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; }

    [ForeignKey(nameof(PipelineId))]
    public Pipeline? Pipeline { get; set; }

    [ForeignKey(nameof(FromLocationId))]
    public ProcessLocation? FromLocation { get; set; }

    [ForeignKey(nameof(ToLocationId))]
    public ProcessLocation? ToLocation { get; set; }

    public ICollection<ProcessTransferFluid> Fluids { get; set; } = new List<ProcessTransferFluid>();
}
