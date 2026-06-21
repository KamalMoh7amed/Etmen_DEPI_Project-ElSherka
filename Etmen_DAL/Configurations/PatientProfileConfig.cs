
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