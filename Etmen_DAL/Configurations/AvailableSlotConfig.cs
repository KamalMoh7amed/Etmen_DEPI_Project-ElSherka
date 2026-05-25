using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class AvailableSlotConfig : IEntityTypeConfiguration<AvailableSlot>
    {
        public void Configure(EntityTypeBuilder<AvailableSlot> builder)
        {
            // AvailableSlotConfiguration
            builder.ToTable("AvailableSlots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.SlotDate)
                .IsRequired();

            builder.Property(x => x.SlotStart)
                .IsRequired();

            builder.Property(x => x.SlotEnd)
                .IsRequired();

            builder.Property(x => x.IsBooked)
                .HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(x => new { x.DoctorProfileId, x.SlotDate, x.SlotStart })
                .IsUnique();

            builder.HasOne(x => x.DoctorProfile)
        .WithMany(x => x.AvailableSlots)
        .HasForeignKey(x => x.DoctorProfileId)
        .OnDelete(DeleteBehavior.Restrict);
        }
    }
}