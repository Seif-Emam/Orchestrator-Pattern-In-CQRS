using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrchestratorPattern.Api.Common.Domain.Entities;

namespace OrchestratorPattern.Api.Common.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderId)
            .IsRequired();

        builder.Property(s => s.TrackingNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Carrier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => s.OrderId)
            .IsUnique();

        builder.HasIndex(s => s.TrackingNumber);
        builder.HasIndex(s => s.Status);
    }
}
