using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class LabResultConfig : IEntityTypeConfiguration<LabResult>
    {
        public void Configure(EntityTypeBuilder<LabResult> builder)
        {
            builder.HasCheckConstraint("CK_LabResult_File", "FileUrl IS NOT NULL OR FilePath IS NOT NULL");

            builder.Property(l => l.TestName).HasMaxLength(200);
            builder.Property(l => l.OcrExtractedData).HasColumnType("nvarchar(max)");

            builder.HasOne(l => l.PatientProfile)
                   .WithMany(p => p.LabResults)
                   .HasForeignKey(l => l.PatientProfileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(l => l.TestDate);
        }
    }
}