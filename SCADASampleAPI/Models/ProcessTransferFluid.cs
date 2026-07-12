using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Per-product entry on a transfer leg. Multiple entries per transfer allow one pipe segment to carry several fluids simultaneously.</summary>
[Table("ScadaProcessTransferFluids")]
public class ProcessTransferFluid
{
    public int ProcessTransferFluidId { get; set; }

    public int ProcessTransferId { get; set; }

    [MaxLength(32)]
    public string FluidCode { get; set; } = "";

    /// <summary>Fraction of this transfer's MaxFlowRate allocated to this fluid (all fractions on a leg sum to 1.0).</summary>
    public double FlowRateFraction { get; set; } = 1.0;

    /// <summary>Relative weight used to allocate source fluid across competing outgoing legs (same role as the old OutflowWeight).</summary>
    public double OutflowWeight { get; set; } = 1.0;

    /// <summary>Computed actual flow rate for this fluid during the last simulation tick (m³/h).</summary>
    public double CurrentFlowRate { get; set; }

    [ForeignKey(nameof(ProcessTransferId))]
    public ProcessTransfer? Transfer { get; set; }
}
