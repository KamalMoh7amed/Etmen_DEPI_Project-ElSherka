using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class DoctorProfileConfig : IEntityTypeConfiguration<DoctorProfile>
    {
        public void Configure(EntityTypeBuilder<DoctorProfile> builder)
        {
            builder.ToTable("DoctorProfiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ApplicationUserId)
                .IsRequired();

           
            builder.HasOne(x => x.ApplicationUser)
                .WithOne() 
                .HasForeignKey<DoctorProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.FullName).HasMaxLength(200);
            builder.Property(x => x.Specialization).HasMaxLength(300);
            builder.Property(x => x.LicenseNumber).HasMaxLength(100);
            builder.Property(x => x.Bio).HasMaxLength(500);
            builder.Property(x => x.ConsultationFee).HasColumnType("decimal(10,2)");
            builder.Property(x => x.CreatedAt).IsRequired();

            // ❌ تم حذف أسطر الـ HasMany(x => x.Appointments) والـ AvailableSlots من هنا لمنع 

            builder.HasIndex(x => x.LicenseNumber).IsUnique();
        }
    }
}