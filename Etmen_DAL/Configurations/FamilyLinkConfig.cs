
namespace Etmen_DAL.Configurations
{
    public class FamilyLinkConfig : IEntityTypeConfiguration<FamilyLink>
    {
        public void Configure(EntityTypeBuilder<FamilyLink> builder)
        {
            builder.ToTable("FamilyLinks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PrimaryPatientId)
                .IsRequired();

            builder.Property(x => x.LinkedPatientId)
                .IsRequired();

       
            builder.HasOne(x => x.PrimaryPatient)
                .WithMany(p => p.PrimaryLinks)
                .HasForeignKey(x => x.PrimaryPatientId)
                .OnDelete(DeleteBehavior.Cascade);

           
            builder.HasOne(x => x.LinkedPatient)
                .WithMany(p => p.LinkedLinks)
                .HasForeignKey(x => x.LinkedPatientId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Property(x => x.Relationship)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.InviteToken)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.InviteToken).IsUnique();
            builder.HasIndex(x => new { x.PrimaryPatientId, x.LinkedPatientId }).IsUnique();
        }
    }
}