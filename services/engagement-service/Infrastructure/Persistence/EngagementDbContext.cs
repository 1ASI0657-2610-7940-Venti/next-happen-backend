using Microsoft.EntityFrameworkCore;
using NextHappen.Engagement.Domain.Entities;

namespace NextHappen.Engagement.Infrastructure.Persistence;

public class EngagementDbContext : DbContext
{
    public EngagementDbContext(DbContextOptions<EngagementDbContext> options) : base(options) { }

    public DbSet<SavedEvent> SavedEvents { get; set; } = null!;
    public DbSet<Metric> Metrics { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SavedEvent>(entity =>
        {
            entity.ToTable("SavedEvents");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.UserId, s.EventId }).IsUnique();
        });

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.ToTable("Metrics");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedOnAdd();
            entity.Property(m => m.Action).HasMaxLength(100).IsRequired();
        });
    }
}
