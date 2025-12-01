using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nexthappen_backend.CreateEvent.Domain.Entities;

namespace nexthappen_backend.CreateEvent.Infrastructure.Persistence.Configuration;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.OwnsOne(e => e.DateRange, dr =>
        {
            dr.Property(d => d.StartDate).HasColumnName("StartDate");
            dr.Property(d => d.EndDate).HasColumnName("EndDate");
        });

        builder.Property(e => e.Title).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Category).HasMaxLength(100);

        builder.Property(e => e.Photos)
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
    }
}