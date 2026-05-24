using System.ComponentModel.DataAnnotations;
using Etmen_Domain.Enums;

namespace Etmen_Domain.Entities
{
    public class Alert
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        [StringLength(300)]
        public string Title { get; set; } = string.Empty;
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public AlertStatus Status { get; set; } = AlertStatus.Unread;
        public string AlertType { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}