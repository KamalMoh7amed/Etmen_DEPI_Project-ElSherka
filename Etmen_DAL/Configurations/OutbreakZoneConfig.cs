
namespace Etmen_DAL.Configurations
{
    public class OutbreakZoneConfig : IEntityTypeConfiguration<OutbreakZone>
    {
        public void Configure(EntityTypeBuilder<OutbreakZone> builder)
        {
            builder.Property(z => z.PolygonCoordinatesJson).HasColumnType("nvarchar(max)");

            builder.Property(z => z.ZoneName).HasMaxLength(150);
            builder.Property(z => z.CenterLatitude).HasColumnType("decimal(9,6)");
            builder.Property(z => z.CenterLongitude).HasColumnType("decimal(9,6)");

            builder.HasOne(z => z.CrisisConfiguration)
                   .WithMany(c => c.OutbreakZones)
                   .HasForeignKey(z => z.CrisisConfigurationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}