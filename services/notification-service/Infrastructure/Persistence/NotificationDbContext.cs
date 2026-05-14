using Microsoft.EntityFrameworkCore;
using NextHappen.Notification.Domain.Entities;

namespace NextHappen.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.Entities.Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).ValueGeneratedOnAdd();
            entity.Property(n => n.Message).IsRequired().HasMaxLength(300);
            entity.Property(n => n.Timestamp).IsRequired();
        });
    }
}
