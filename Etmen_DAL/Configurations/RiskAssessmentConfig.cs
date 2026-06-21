
namespace Etmen_DAL.Configurations
{
    public class RiskAssessmentConfig : IEntityTypeConfiguration<RiskAssessment>
    {
        public void Configure(EntityTypeBuilder<RiskAssessment> builder)
        {
            builder.ToTable(t => t.HasCheckConstraint("CK_RiskAssessment_Score", "RiskScore >= 0 AND RiskScore <= 1"));

            builder.Property(r => r.RecommendationsJson).HasColumnType("nvarchar(max)");

            builder.HasOne(r => r.PatientProfile)
                   .WithMany(p => p.RiskAssessments)
                   .HasForeignKey(r => r.PatientProfileId)
                   .OnDelete(DeleteBehavior.Restrict); 

            builder.HasIndex(r => r.AssessmentDate);
        }
    }
}