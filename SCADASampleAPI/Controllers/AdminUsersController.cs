using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Contracts;
using SCADASampleAPI.Data;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminUsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserListItemDto>>> ListUsers()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var result = new List<UserListItemDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                Roles = roles.ToList()
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> CreateUser([FromBody] RegisterUserRequest request)
    {
        if (!DbInitializer.Roles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            return BadRequest($"Role must be one of: {string.Join(", ", DbInitializer.Roles)}");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            return BadRequest(create.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, request.Role);

        var roles = await _userManager.GetRolesAsync(user);
        return StatusCode(StatusCodes.Status201Created, new UserListItemDto
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            Roles = roles.ToList()
        });
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(string id, [FromBody] UpdateUserRolesRequest request)
    {
        foreach (var r in request.Roles)
        {
            if (!DbInitializer.Roles.Contains(r, StringComparer.OrdinalIgnoreCase))
                return BadRequest($"Unknown role: {r}");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        if (request.Roles.Count > 0)
            await _userManager.AddToRolesAsync(user, request.Roles);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (adminUsers.Count <= 1)
                return BadRequest("Cannot delete the last admin user.");
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors.Select(e => e.Description));
    }
}
