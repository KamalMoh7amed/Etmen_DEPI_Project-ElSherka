using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class PatientProfileConfig : IEntityTypeConfiguration<PatientProfile>
    {
        public void Configure(EntityTypeBuilder<PatientProfile> builder)
        {
            builder.HasOne(p => p.ApplicationUser)
                   .WithOne(u => u.PatientProfile)
                   .HasForeignKey<PatientProfile>(p => p.ApplicationUserId)
                   .OnDelete(DeleteBehavior.Cascade);

            //  BMI Computed Column ( Height و Weight)
            //builder.Property(p => p.BMI)
            //       .HasComputedColumnSql("CASE WHEN Height IS NULL OR Height = 0 THEN 0 ELSE ROUND(Weight / POWER(Height / 100.0, 2), 2) END", stored: true);

            builder.Property(p => p.FullName).HasMaxLength(150);
            builder.Property(p => p.Gender).HasMaxLength(10);
            builder.Property(p => p.BloodType).HasMaxLength(5);
            builder.Property(p => p.ChronicDiseasesNotes).HasMaxLength(1000);
            builder.Property(p => p.Allergies).HasMaxLength(1000);
            builder.Property(p => p.CurrentMedications).HasMaxLength(1000);

            builder.HasIndex(p => p.ApplicationUserId).IsUnique();
        }
    }
}