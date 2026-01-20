using BookingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingPlatform.Infrastructure.Persistence.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("StaffMembers");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.TenantUserId);

        builder.Property(s => s.Title)
            .HasMaxLength(100);

        builder.HasOne(s => s.TenantUser)
            .WithOne(tu => tu.StaffMember)
            .HasForeignKey<StaffMember>(s => s.TenantUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.StaffMembers)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
