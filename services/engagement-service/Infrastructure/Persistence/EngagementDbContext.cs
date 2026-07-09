using Microsoft.EntityFrameworkCore;
using NextHappen.Engagement.Domain.Entities;

namespace NextHappen.Engagement.Infrastructure.Persistence;

public class EngagementDbContext : DbContext
{
    public EngagementDbContext(DbContextOptions<EngagementDbContext> options) : base(options) { }

    public DbSet<SavedEvent> SavedEvents { get; set; } = null!;
    public DbSet<Metric> Metrics { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;

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

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.UserName).HasMaxLength(150);
            entity.Property(r => r.Comment).HasMaxLength(1000);
            entity.HasIndex(r => r.EventId);
            entity.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
        });
    }
}
