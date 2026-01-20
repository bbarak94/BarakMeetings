using BookingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingPlatform.Infrastructure.Persistence.Configurations;

public class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHours>
{
    public void Configure(EntityTypeBuilder<WorkingHours> builder)
    {
        builder.ToTable("WorkingHours");

        builder.HasKey(wh => wh.Id);

        builder.HasIndex(wh => new { wh.StaffMemberId, wh.DayOfWeek });

        builder.HasOne(wh => wh.StaffMember)
            .WithMany(s => s.WorkingHours)
            .HasForeignKey(wh => wh.StaffMemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
