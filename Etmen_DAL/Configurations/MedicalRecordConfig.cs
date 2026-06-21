
namespace Etmen_DAL.Configurations
{
    public class MedicalRecordConfig : IEntityTypeConfiguration<MedicalRecord>
    {
        public void Configure(EntityTypeBuilder<MedicalRecord> builder)
        {
            builder.HasOne(m => m.PatientProfile)
                   .WithMany(p => p.MedicalRecords)
                   .HasForeignKey(m => m.PatientProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(m => m.Symptoms).HasMaxLength(1000);
            builder.Property(m => m.Notes).HasMaxLength(1000);
            builder.HasIndex(m => m.RecordDate);
        }
    }
}