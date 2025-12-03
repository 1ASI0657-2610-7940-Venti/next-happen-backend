using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using nexthappen_backend.Notifications.Domain.Entities;

namespace nexthappen_backend.Notifications.Infrastructure.Persistence.Configuration;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(n => n.Timestamp)
            .IsRequired();
    }
}