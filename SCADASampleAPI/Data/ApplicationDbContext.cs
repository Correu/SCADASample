using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SCADASampleAPI.Models;

namespace SCADASampleAPI.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Alarm> Alarms => Set<Alarm>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SQL Server: two CASCADE paths Pipeline -> Alarm (direct) and Pipeline -> Tag -> Alarm
        // triggers "multiple cascade paths". Keep Cascade on Pipeline; use Restrict on Tag -> Alarm.
        builder.Entity<Alarm>()
            .HasOne(a => a.Tag)
            .WithMany(t => t.Alarms)
            .HasForeignKey(a => a.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
