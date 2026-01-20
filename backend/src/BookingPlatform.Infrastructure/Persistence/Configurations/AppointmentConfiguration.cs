using BookingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingPlatform.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.StaffMemberId, a.StartTimeUtc, a.EndTimeUtc });
        builder.HasIndex(a => new { a.TenantId, a.StartTimeUtc });
        builder.HasIndex(a => a.GroupSessionId);

        builder.Property(a => a.Price)
            .HasPrecision(10, 2);

        builder.Property(a => a.Version)
            .IsRowVersion();

        builder.HasOne(a => a.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.StaffMember)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Client)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Tenant)
            .WithMany(t => t.Appointments)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
