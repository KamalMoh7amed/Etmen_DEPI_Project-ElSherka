namespace Etmen_DAL.Configurations
{
    public class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            
            builder.ToTable("AspNetUsers");

            builder.Property(x => x.FirstName)
                .HasMaxLength(100);

            builder.Property(x => x.LastName)
                .HasMaxLength(100);

            builder.Property(x => x.ProfilePicture)
                .HasMaxLength(500);

            builder.Property(x => x.IsEmailVerified)
                .HasDefaultValue(false);

            builder.Property(x => x.VerificationToken)
                .HasMaxLength(500);

            builder.Property(x => x.ResetPasswordToken)
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}