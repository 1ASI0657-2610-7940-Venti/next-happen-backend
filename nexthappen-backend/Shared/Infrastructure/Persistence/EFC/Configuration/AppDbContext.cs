using EntityFrameworkCore.CreatedUpdatedDate.Extensions;
using Microsoft.EntityFrameworkCore;
using nexthappen_backend.AssignStands.Domain.Entities;
using nexthappen_backend.CreateEvent.Domain.Entities;
using nexthappen_backend.IAM.Domain.Entities;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

namespace nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Event> Events { get; set; } = null!;
    
    public DbSet<AssignedStand> AssignedStands { get; set; }
    
    public DbSet<User> Users { get; set; } = null!;
    
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddCreatedUpdatedInterceptor();
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Organizer).IsRequired(false);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Description).IsRequired(false);
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10,2)")
                .IsRequired(false);
            entity.Property(e => e.Quantity).IsRequired(false);
            entity.Property(e => e.Category).IsRequired(false);
            entity.Property(e => e.Address).IsRequired(false);
            entity.Property(e => e.Location).IsRequired(false);

            //Guardar las fotos como JSON en MySQL (soporta imágenes Base64 grandes)
            entity.Property<string>("PhotosJson")
                .HasColumnType("longtext"); // <-- aquí está la diferencia clave

            entity.Ignore(e => e.Photos); // EF no mapea directamente la lista
            entity.OwnsOne(e => e.DateRange, dr =>
            {
                dr.Property(d => d.StartDate)
                  .HasColumnName("StartDate")
                  .HasColumnType("datetime")
                  .IsRequired();

                dr.Property(d => d.EndDate)
                  .HasColumnName("EndDate")
                  .HasColumnType("datetime")
                  .IsRequired();
            });
        });
        
        modelBuilder.Entity<AssignedStand>(entity =>
        {
            entity.ToTable("AssignedStands");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id)
                .ValueGeneratedNever(); // porque ya lo creas con Guid.NewGuid()

            entity.Property(s => s.EventId)
                .IsRequired();

            entity.Property(s => s.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(s => s.Category)
                .HasMaxLength(150)
                .IsRequired();
        });
    }
    

}