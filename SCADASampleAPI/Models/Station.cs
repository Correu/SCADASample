using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCADASampleAPI.Models;

/// <summary>Physical station facility grouping one or more process locations under a pipeline.</summary>
[Table("ScadaStations")]
public class Station
{
    public int StationId { get; set; }

    public int PipelineId { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = "";

    [MaxLength(32)]
    public string Code { get; set; } = "";

    public double LayoutX { get; set; }

    public double LayoutY { get; set; }

    [ForeignKey(nameof(PipelineId))]
    public Pipeline? Pipeline { get; set; }

    public ICollection<ProcessLocation> Locations { get; set; } = new List<ProcessLocation>();
}
