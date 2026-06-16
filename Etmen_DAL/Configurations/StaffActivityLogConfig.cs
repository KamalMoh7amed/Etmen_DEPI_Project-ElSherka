using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class StaffActivityLogConfig : IEntityTypeConfiguration<StaffActivityLog>
    {
        public void Configure(EntityTypeBuilder<StaffActivityLog> builder)
        {
            builder.ToTable("StaffActivityLogs");

            builder.HasOne(x => x.StaffProfile)
                .WithMany()
                .HasForeignKey(x => x.StaffProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Action)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Details)
                .HasMaxLength(1000);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
