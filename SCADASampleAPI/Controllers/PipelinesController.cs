using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Contracts;
using SCADASampleAPI.Data;

namespace SCADASampleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PipelinesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PipelineSummaryDto>>> GetPipelines()
    {
        var list = await context.Pipelines
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new PipelineSummaryDto
            {
                PipelineId = p.PipelineId,
                Name = p.Name,
                Code = p.Code,
                IsActive = p.IsActive,
                TagCount = p.Tags.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PipelineSummaryDto>> GetPipeline(int id)
    {
        var p = await context.Pipelines.AsNoTracking()
            .Where(x => x.PipelineId == id)
            .Select(x => new PipelineSummaryDto
            {
                PipelineId = x.PipelineId,
                Name = x.Name,
                Code = x.Code,
                IsActive = x.IsActive,
                TagCount = x.Tags.Count
            })
            .FirstOrDefaultAsync();

        return p == null ? NotFound() : Ok(p);
    }

    [HttpGet("{id:int}/tags")]
    public async Task<ActionResult<IEnumerable<TagSnapshotDto>>> GetTags(int id)
    {
        var exists = await context.Pipelines.AnyAsync(p => p.PipelineId == id);
        if (!exists) return NotFound();

        var tags = await context.Tags.AsNoTracking()
            .Where(t => t.PipelineId == id)
            .OrderBy(t => t.Name)
            .Select(t => new TagSnapshotDto
            {
                TagId = t.TagId,
                PipelineId = t.PipelineId,
                Name = t.Name,
                Unit = t.Unit,
                MinValue = t.MinValue,
                MaxValue = t.MaxValue,
                CurrentValue = t.CurrentValue,
                LastUpdatedUtc = t.LastUpdatedUtc
            })
            .ToListAsync();

        return Ok(tags);
    }

    [HttpGet("{id:int}/process-graph")]
    public async Task<ActionResult<ProcessGraphDto>> GetProcessGraph(int id)
    {
        var exists = await context.Pipelines.AnyAsync(p => p.PipelineId == id);
        if (!exists) return NotFound();

        var locations = await context.ProcessLocations.AsNoTracking()
            .Where(l => l.PipelineId == id)
            .OrderBy(l => l.Code)
            .Select(l => new ProcessLocationDto
            {
                ProcessLocationId = l.ProcessLocationId,
                PipelineId = l.PipelineId,
                StationId = l.StationId,
                Name = l.Name,
                Code = l.Code,
                Kind = l.Kind,
                Capacity = l.Capacity,
                CurrentVolume = l.CurrentVolume,
                LayoutX = l.LayoutX,
                LayoutY = l.LayoutY,
                LastUpdatedUtc = l.LastUpdatedUtc,
                Fluids = l.Fluids
                    .OrderBy(f => f.FluidCode)
                    .Select(f => new ProcessLocationFluidDto { FluidCode = f.FluidCode, Volume = f.Volume })
                    .ToList()
            })
            .ToListAsync();

        var transfers = await context.ProcessTransfers.AsNoTracking()
            .Where(t => t.PipelineId == id)
            .OrderBy(t => t.ProcessTransferId)
            .Select(t => new ProcessTransferDto
            {
                ProcessTransferId = t.ProcessTransferId,
                PipelineId = t.PipelineId,
                FromLocationId = t.FromLocationId,
                ToLocationId = t.ToLocationId,
                IsPumpRunning = t.IsPumpRunning,
                ValveOpen = t.ValveOpen,
                MaxFlowRate = t.MaxFlowRate,
                CurrentFlowRate = t.CurrentFlowRate,
                LastUpdatedUtc = t.LastUpdatedUtc,
                Fluids = t.Fluids
                    .OrderBy(f => f.FluidCode)
                    .Select(f => new TransferFluidDto
                    {
                        FluidCode = f.FluidCode,
                        FlowRateFraction = f.FlowRateFraction,
                        CurrentFlowRate = f.CurrentFlowRate
                    })
                    .ToList()
            })
            .ToListAsync();

        var stations = await context.Stations.AsNoTracking()
            .Where(s => s.PipelineId == id)
            .OrderBy(s => s.Code)
            .Select(s => new StationDto
            {
                StationId = s.StationId,
                PipelineId = s.PipelineId,
                Name = s.Name,
                Code = s.Code,
                LayoutX = s.LayoutX,
                LayoutY = s.LayoutY,
                Locations = s.Locations
                    .OrderBy(l => l.Code)
                    .Select(l => new ProcessLocationDto
                    {
                        ProcessLocationId = l.ProcessLocationId,
                        PipelineId = l.PipelineId,
                        StationId = l.StationId,
                        Name = l.Name,
                        Code = l.Code,
                        Kind = l.Kind,
                        Capacity = l.Capacity,
                        CurrentVolume = l.CurrentVolume,
                        LayoutX = l.LayoutX,
                        LayoutY = l.LayoutY,
                        LastUpdatedUtc = l.LastUpdatedUtc,
                        Fluids = l.Fluids
                            .OrderBy(f => f.FluidCode)
                            .Select(f => new ProcessLocationFluidDto { FluidCode = f.FluidCode, Volume = f.Volume })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new ProcessGraphDto { Locations = locations, Transfers = transfers, Stations = stations });
    }

    [HttpGet("{id:int}/stations")]
    public async Task<ActionResult<IEnumerable<StationDto>>> GetStations(int id)
    {
        var exists = await context.Pipelines.AnyAsync(p => p.PipelineId == id);
        if (!exists) return NotFound();

        var stations = await context.Stations.AsNoTracking()
            .Where(s => s.PipelineId == id)
            .OrderBy(s => s.Code)
            .Select(s => new StationDto
            {
                StationId = s.StationId,
                PipelineId = s.PipelineId,
                Name = s.Name,
                Code = s.Code,
                LayoutX = s.LayoutX,
                LayoutY = s.LayoutY,
                Locations = s.Locations
                    .OrderBy(l => l.Code)
                    .Select(l => new ProcessLocationDto
                    {
                        ProcessLocationId = l.ProcessLocationId,
                        PipelineId = l.PipelineId,
                        StationId = l.StationId,
                        Name = l.Name,
                        Code = l.Code,
                        Kind = l.Kind,
                        Capacity = l.Capacity,
                        CurrentVolume = l.CurrentVolume,
                        LayoutX = l.LayoutX,
                        LayoutY = l.LayoutY,
                        LastUpdatedUtc = l.LastUpdatedUtc,
                        Fluids = l.Fluids
                            .OrderBy(f => f.FluidCode)
                            .Select(f => new ProcessLocationFluidDto { FluidCode = f.FluidCode, Volume = f.Volume })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(stations);
    }

    [HttpPost("{id:int}/transfers/{transferId:int}/pump")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> SetTransferPump(int id, int transferId, [FromBody] PumpStateRequest body)
    {
        var transfer = await context.ProcessTransfers
            .FirstOrDefaultAsync(t => t.ProcessTransferId == transferId && t.PipelineId == id);

        if (transfer == null) return NotFound();

        transfer.IsPumpRunning = body.Running;
        transfer.LastUpdatedUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/transfers/{transferId:int}/valve")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> SetTransferValve(int id, int transferId, [FromBody] ValveStateRequest body)
    {
        var transfer = await context.ProcessTransfers
            .FirstOrDefaultAsync(t => t.ProcessTransferId == transferId && t.PipelineId == id);

        if (transfer == null) return NotFound();

        transfer.ValveOpen = body.Open;
        transfer.LastUpdatedUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await context.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Code = p.Code,
                Name = p.Name,
                HexColor = p.HexColor,
                ProductType = p.ProductType,
                Density = p.Density
            })
            .ToListAsync();

        return Ok(products);
    }
}
