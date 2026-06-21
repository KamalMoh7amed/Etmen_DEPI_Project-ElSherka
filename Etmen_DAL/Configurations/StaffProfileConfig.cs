namespace Etmen_DAL.Configurations
{
    public class StaffProfileConfig : IEntityTypeConfiguration<StaffProfile>
    {
        public void Configure(EntityTypeBuilder<StaffProfile> builder)
        {
            builder.ToTable("StaffProfiles");

            builder.HasOne(x => x.ApplicationUser)
                .WithOne(x => x.StaffProfile)
                .HasForeignKey<StaffProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.HealthcareProvider)
                .WithMany()
                .HasForeignKey(x => x.HealthcareProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.InvitationSenderUserId)
                .HasMaxLength(450);

            builder.Property(x => x.InvitationToken)
                .HasMaxLength(200);
        }
    }
}
