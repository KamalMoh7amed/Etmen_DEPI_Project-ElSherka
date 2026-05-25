using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class AppointmentConfig : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            // AppointmentConfiguration
            builder.ToTable("Appointments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AppointmentDate)
                .IsRequired();

            builder.Property(x => x.StartTime)
                .IsRequired();

            builder.Property(x => x.EndTime)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(AppointmentStatus.Scheduled);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.PatientProfile)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.PatientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.DoctorProfile)
        .WithMany(x => x.Appointments)
        .HasForeignKey(x => x.DoctorProfileId)
        .OnDelete(DeleteBehavior.NoAction);
        }
    }
}