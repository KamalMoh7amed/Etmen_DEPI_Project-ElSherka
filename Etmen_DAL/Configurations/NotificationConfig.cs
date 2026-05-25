using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class NotificationConfig : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasOne(n => n.User)
                   .WithMany(u => u.Notifications)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(n => n.Title).HasMaxLength(200);
            builder.Property(n => n.Message).HasMaxLength(1000);
            builder.Property(n => n.Link).HasMaxLength(500);

            builder.HasIndex(n => new { n.UserId, n.IsRead });
        }
    }
}