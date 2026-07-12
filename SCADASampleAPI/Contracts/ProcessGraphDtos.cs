namespace SCADASampleAPI.Contracts;

public class ProductDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string HexColor { get; set; } = "";
    public string ProductType { get; set; } = "";
    public double Density { get; set; }
}

public class ProcessLocationFluidDto
{
    public string FluidCode { get; set; } = "";
    public double Volume { get; set; }
}

public class TransferFluidDto
{
    public string FluidCode { get; set; } = "";
    public double FlowRateFraction { get; set; }
    public double CurrentFlowRate { get; set; }
}

public class ProcessLocationDto
{
    public int ProcessLocationId { get; set; }
    public int PipelineId { get; set; }
    public int? StationId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Kind { get; set; }
    public double Capacity { get; set; }
    public double CurrentVolume { get; set; }
    public double LayoutX { get; set; }
    public double LayoutY { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
    public List<ProcessLocationFluidDto> Fluids { get; set; } = new();
}

public class ProcessTransferDto
{
    public int ProcessTransferId { get; set; }
    public int PipelineId { get; set; }
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }
    public bool IsPumpRunning { get; set; }
    public bool ValveOpen { get; set; }
    public double MaxFlowRate { get; set; }
    public double CurrentFlowRate { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
    public List<TransferFluidDto> Fluids { get; set; } = new();
}

public class StationDto
{
    public int StationId { get; set; }
    public int PipelineId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public double LayoutX { get; set; }
    public double LayoutY { get; set; }
    public List<ProcessLocationDto> Locations { get; set; } = new();
}

public class ProcessGraphDto
{
    public List<ProcessLocationDto> Locations { get; set; } = new();
    public List<ProcessTransferDto> Transfers { get; set; } = new();
    public List<StationDto> Stations { get; set; } = new();
}

public class LocationUpdateDto
{
    public int PipelineId { get; set; }
    public int ProcessLocationId { get; set; }
    public int? StationId { get; set; }
    public double CurrentVolume { get; set; }
    public double Capacity { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
    public List<ProcessLocationFluidDto> Fluids { get; set; } = new();
}

public class TransferUpdateDto
{
    public int PipelineId { get; set; }
    public int ProcessTransferId { get; set; }
    public double CurrentFlowRate { get; set; }
    public bool IsPumpRunning { get; set; }
    public bool ValveOpen { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
    public List<TransferFluidDto> Fluids { get; set; } = new();
}

public class PumpStateRequest
{
    public bool Running { get; set; }
}

public class ValveStateRequest
{
    public bool Open { get; set; }
}
