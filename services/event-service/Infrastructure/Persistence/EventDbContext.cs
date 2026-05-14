using Microsoft.EntityFrameworkCore;
using NextHappen.Event.Domain.Entities;

namespace NextHappen.Event.Infrastructure.Persistence;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Event> Events { get; set; } = null!;
    public DbSet<AssignedStand> AssignedStands { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EVENTS
        modelBuilder.Entity<Domain.Entities.Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Organizer).IsRequired(false);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Description).IsRequired(false);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Quantity).IsRequired(false);
            entity.Property(e => e.Category).IsRequired(false);
            entity.Property(e => e.Address).IsRequired(false);
            entity.Property(e => e.Location).IsRequired(false);

            entity.Property(e => e.Photos)
                .HasConversion(
                    v => string.Join(";", v ?? new List<string>()),
                    v => (v ?? "")
                        .Split(";", StringSplitOptions.RemoveEmptyEntries)
                        .ToList()
                )
                .HasColumnType("longtext");

            entity.OwnsOne(e => e.DateRange, dr =>
            {
                dr.Property(d => d.StartDate).HasColumnName("StartDate").HasColumnType("datetime");
                dr.Property(d => d.EndDate).HasColumnName("EndDate").HasColumnType("datetime");
            });
        });

        // ASSIGNED STANDS
        modelBuilder.Entity<AssignedStand>(entity =>
        {
            entity.ToTable("AssignedStands");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.EventId).IsRequired();
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Category).HasMaxLength(150).IsRequired();
        });
    }
}
