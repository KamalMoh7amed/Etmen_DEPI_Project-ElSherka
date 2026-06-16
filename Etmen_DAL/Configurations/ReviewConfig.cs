using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class ReviewConfig : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Rating)
                .IsRequired();

            builder.Property(r => r.Comment)
                .HasMaxLength(1000);

            builder.Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(r => r.PatientProfile)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.PatientProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.DoctorProfile)
                .WithMany(d => d.Reviews)
                .HasForeignKey(r => r.DoctorProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(r => r.HealthcareProvider)
                .WithMany(h => h.Reviews)
                .HasForeignKey(r => r.HealthcareProviderId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
