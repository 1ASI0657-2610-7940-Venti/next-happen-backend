using EntityFrameworkCore.CreatedUpdatedDate.Extensions;
using Microsoft.EntityFrameworkCore;
using nexthappen_backend.AssignStands.Domain.Entities;
using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.IAM.Domain.Entities;
using nexthappen_backend.Metrics.Domain.Entities;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<AssignedStand> AssignedStands { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    
    public DbSet<Metric> Metrics { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddCreatedUpdatedInterceptor();
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new MetricConfiguration());

        // EVENTS
        modelBuilder.Entity<Event>(entity =>
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
                    v => string.Join(";", v),
                    v => v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList()
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
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.EventId).IsRequired();
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Category).HasMaxLength(150).IsRequired();
        });

        // USERS (IAM) 👈 **SOLUCIÓN**
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id).ValueGeneratedNever();
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
        });
    }
}
