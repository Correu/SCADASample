using System.ComponentModel.DataAnnotations;

namespace SCADASampleAPI.Models;

public class Pipeline
{
    public int PipelineId { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = "";

    [MaxLength(32)]
    public string Code { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
}
