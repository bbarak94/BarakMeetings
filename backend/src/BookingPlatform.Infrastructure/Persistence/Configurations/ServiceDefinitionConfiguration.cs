using BookingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingPlatform.Infrastructure.Persistence.Configurations;

public class ServiceDefinitionConfiguration : IEntityTypeConfiguration<ServiceDefinition>
{
    public void Configure(EntityTypeBuilder<ServiceDefinition> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.TenantId);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.BasePrice)
            .HasPrecision(10, 2);

        builder.Property(s => s.Color)
            .HasMaxLength(7); // #FFFFFF

        builder.Ignore(s => s.IsGroupClass);

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Services)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
