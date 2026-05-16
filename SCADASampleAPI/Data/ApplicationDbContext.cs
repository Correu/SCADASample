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
    public DbSet<ProcessLocation> ProcessLocations => Set<ProcessLocation>();
    public DbSet<ProcessTransfer> ProcessTransfers => Set<ProcessTransfer>();
    public DbSet<ProcessLocationFluid> ProcessLocationFluids => Set<ProcessLocationFluid>();

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

        // Two FKs Transfer -> Location: avoid multiple cascade paths from Pipeline; use Restrict on location FKs.
        builder.Entity<ProcessLocation>()
            .HasOne(l => l.Pipeline)
            .WithMany(p => p.ProcessLocations)
            .HasForeignKey(l => l.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProcessTransfer>()
            .HasOne(t => t.Pipeline)
            .WithMany(p => p.ProcessTransfers)
            .HasForeignKey(t => t.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProcessTransfer>()
            .HasOne(t => t.FromLocation)
            .WithMany()
            .HasForeignKey(t => t.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcessTransfer>()
            .HasOne(t => t.ToLocation)
            .WithMany()
            .HasForeignKey(t => t.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcessLocationFluid>()
            .HasOne(f => f.ProcessLocation)
            .WithMany(l => l.Fluids)
            .HasForeignKey(f => f.ProcessLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProcessLocationFluid>()
            .HasIndex(f => new { f.ProcessLocationId, f.FluidCode })
            .IsUnique();
    }
}
