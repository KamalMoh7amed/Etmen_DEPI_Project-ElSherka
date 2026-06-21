

namespace Etmen_DAL.Configurations
{
    public class CrisisConfigurationConfig : IEntityTypeConfiguration<CrisisConfiguration>
    {
        public void Configure(EntityTypeBuilder<CrisisConfiguration> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CrisisName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.EmergencyThreshold)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.HighRiskThreshold)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.MediumRiskThreshold)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.StartDate)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.OwnsMany(x => x.SymptomWeights, sw =>
            {
                
                sw.ToTable("CrisisSymptomWeights"); 

               
                sw.Property(s => s.SymptomName).HasMaxLength(200).IsRequired();
                sw.Property(s => s.Weight).HasColumnType("decimal(3,2)");
            });

           
            builder.HasMany(x => x.OutbreakZones)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.CrisisName);
        }
    }
}