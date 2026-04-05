using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Data;

public static class DbInitializer
{
    public static readonly string[] Roles = ["Admin", "Operator", "Viewer"];

    /// <summary>Dev-only default admin. Must use a dotted domain so <see cref="System.ComponentModel.DataAnnotations.EmailAddressAttribute"/> and Identity accept it (<c>admin@local</c> is rejected).</summary>
    public const string DefaultAdminEmail = "admin@example.com";

    public const string DefaultAdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Seeded default admin user {Email}.", DefaultAdminEmail);
            }
            else
            {
                logger.LogError(
                    "Failed to create default admin user: {Errors}",
                    string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
            }
        }
    }
}
