using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Data;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlarmsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Alarm>>> GetAlarms([FromQuery] int? pipelineId)
    {
        var query = context.Alarms.AsNoTracking().Include(a => a.Tag).Include(a => a.Pipeline).AsQueryable();
        if (pipelineId.HasValue)
            query = query.Where(a => a.PipelineId == pipelineId.Value);

        return Ok(await query.OrderByDescending(a => a.RaisedAt).ToListAsync());
    }

    [HttpPost("{id:int}/acknowledge")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var alarm = await context.Alarms.FirstOrDefaultAsync(a => a.AlarmId == id);
        if (alarm == null)
            return NotFound();

        alarm.IsActive = false;
        alarm.AcknowledgedAt = DateTimeOffset.UtcNow;
        alarm.AcknowledgedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
