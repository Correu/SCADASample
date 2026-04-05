namespace SCADASampleAPI.Contracts;

public class TagValueUpdateDto
{
    public int PipelineId { get; set; }
    public int TagId { get; set; }
    public string TagName { get; set; } = "";
    public double Value { get; set; }
    public string Unit { get; set; } = "";
    public DateTimeOffset TimestampUtc { get; set; }
}

public class PipelineSummaryDto
{
    public int PipelineId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public bool IsActive { get; set; }
    public int TagCount { get; set; }
}

public class TagSnapshotDto
{
    public int TagId { get; set; }
    public int PipelineId { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double CurrentValue { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
}
