using Microsoft.EntityFrameworkCore;
using NextHappen.Ticket.Domain.Entities;

namespace NextHappen.Ticket.Infrastructure.Persistence;

public class TicketDbContext : DbContext
{
    public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Ticket> Tickets { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.Entities.Ticket>(entity =>
        {
            entity.ToTable("Tickets");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedNever();
            entity.Property(t => t.Status).HasMaxLength(50).IsRequired();
            entity.Property(t => t.PurchaseDate).IsRequired();
            entity.Property(t => t.Price).HasColumnType("decimal(10,2)");
            entity.Property(t => t.QrCode).HasMaxLength(128);
            entity.Property(t => t.ShortCode).HasMaxLength(16);
            entity.HasIndex(t => t.QrCode).IsUnique();
            entity.HasIndex(t => t.ShortCode).IsUnique();
            entity.HasIndex(t => t.EventId);
            entity.HasIndex(t => t.OrderId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).ValueGeneratedNever();
            entity.Property(o => o.Status).HasMaxLength(50).IsRequired();
            entity.Property(o => o.Currency).HasMaxLength(10).IsRequired();
            entity.Property(o => o.UnitPrice).HasColumnType("decimal(10,2)");
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(o => o.StripeSessionId).HasMaxLength(255);
            entity.Property(o => o.StripePaymentIntentId).HasMaxLength(255);
            entity.HasIndex(o => o.StripeSessionId).IsUnique();
        });
    }
}
