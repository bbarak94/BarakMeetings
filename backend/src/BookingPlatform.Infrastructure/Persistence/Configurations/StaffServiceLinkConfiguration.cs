using BookingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingPlatform.Infrastructure.Persistence.Configurations;

public class StaffServiceLinkConfiguration : IEntityTypeConfiguration<StaffServiceLink>
{
    public void Configure(EntityTypeBuilder<StaffServiceLink> builder)
    {
        builder.ToTable("StaffServiceLinks");

        builder.HasKey(ssl => ssl.Id);

        builder.HasIndex(ssl => new { ssl.StaffMemberId, ssl.ServiceId })
            .IsUnique();

        builder.Property(ssl => ssl.PriceOverride)
            .HasPrecision(10, 2);

        builder.HasOne(ssl => ssl.StaffMember)
            .WithMany(s => s.StaffServiceLinks)
            .HasForeignKey(ssl => ssl.StaffMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ssl => ssl.Service)
            .WithMany(s => s.StaffServiceLinks)
            .HasForeignKey(ssl => ssl.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
