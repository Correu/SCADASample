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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<ProcessLocation> ProcessLocations => Set<ProcessLocation>();
    public DbSet<ProcessTransfer> ProcessTransfers => Set<ProcessTransfer>();
    public DbSet<ProcessLocationFluid> ProcessLocationFluids => Set<ProcessLocationFluid>();
    public DbSet<ProcessTransferFluid> ProcessTransferFluids => Set<ProcessTransferFluid>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Product uses a string PK
        builder.Entity<Product>()
            .HasKey(p => p.Code);

        // SQL Server: two CASCADE paths Pipeline -> Alarm (direct) and Pipeline -> Tag -> Alarm
        // triggers "multiple cascade paths". Keep Cascade on Pipeline; use Restrict on Tag -> Alarm.
        builder.Entity<Alarm>()
            .HasOne(a => a.Tag)
            .WithMany(t => t.Alarms)
            .HasForeignKey(a => a.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        // Station belongs to Pipeline with cascade
        builder.Entity<Station>()
            .HasOne(s => s.Pipeline)
            .WithMany()
            .HasForeignKey(s => s.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Two FKs Transfer -> Location: avoid multiple cascade paths from Pipeline; use Restrict on location FKs.
        builder.Entity<ProcessLocation>()
            .HasOne(l => l.Pipeline)
            .WithMany(p => p.ProcessLocations)
            .HasForeignKey(l => l.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        // StationId is nullable. Use Restrict (NO ACTION) — SetNull would create multiple cascade
        // paths with Pipeline→Stations CASCADE and Pipeline→Locations CASCADE on SQL Server.
        builder.Entity<ProcessLocation>()
            .HasOne(l => l.Station)
            .WithMany(s => s.Locations)
            .HasForeignKey(l => l.StationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

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

        // ProcessTransferFluid cascades from its parent transfer
        builder.Entity<ProcessTransferFluid>()
            .HasOne(f => f.Transfer)
            .WithMany(t => t.Fluids)
            .HasForeignKey(f => f.ProcessTransferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProcessTransferFluid>()
            .HasIndex(f => new { f.ProcessTransferId, f.FluidCode })
            .IsUnique();
    }
}
