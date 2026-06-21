
namespace Etmen_DAL.Configurations
{
    public class DoctorProviderConfig : IEntityTypeConfiguration<DoctorProvider>
    {
        public void Configure(EntityTypeBuilder<DoctorProvider> builder)
        {
            builder.ToTable("DoctorProviders");

            
            builder.HasKey(dp => new { dp.DoctorProfileId, dp.HealthcareProviderId });

            builder.Property(dp => dp.IsEmergencyDoctor)
                .HasDefaultValue(false);

            builder.Property(dp => dp.IsOwner)
                .HasDefaultValue(false);

            builder.Property(dp => dp.AffiliationRole)
                .HasMaxLength(100);

            builder.HasOne(dp => dp.DoctorProfile)
                .WithMany(d => d.DoctorProviders)
                .HasForeignKey(dp => dp.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(dp => dp.HealthcareProvider)
                .WithMany(h => h.DoctorProviders)
                .HasForeignKey(dp => dp.HealthcareProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
