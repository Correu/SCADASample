using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Per-product volume bucket at a process location (m³).</summary>
[Table("ScadaProcessLocationFluids")]
public class ProcessLocationFluid
{
    public int ProcessLocationFluidId { get; set; }

    public int ProcessLocationId { get; set; }

    [MaxLength(32)]
    public string FluidCode { get; set; } = "";

    public double Volume { get; set; }

    [ForeignKey(nameof(ProcessLocationId))]
    public ProcessLocation? ProcessLocation { get; set; }
}
