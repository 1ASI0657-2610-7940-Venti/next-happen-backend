using Microsoft.EntityFrameworkCore;
using NextHappen.Ticket.Domain.Entities;

namespace NextHappen.Ticket.Infrastructure.Persistence;

public class TicketDbContext : DbContext
{
    public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Ticket> Tickets { get; set; } = null!;

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
        });
    }
}
