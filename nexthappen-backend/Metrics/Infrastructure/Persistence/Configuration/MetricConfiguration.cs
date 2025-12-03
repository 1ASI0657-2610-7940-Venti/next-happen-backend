using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nexthappen_backend.Metrics.Domain.Entities;

public class MetricConfiguration : IEntityTypeConfiguration<Metric>
{
    public void Configure(EntityTypeBuilder<Metric> builder)
    {
        builder.ToTable("Metrics");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventId)
            .IsRequired();

        builder.Property(m => m.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Timestamp)
            .IsRequired();
    }
}