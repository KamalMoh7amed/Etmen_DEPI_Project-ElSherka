using System.ComponentModel.DataAnnotations;

namespace Etmen_Domain.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        [StringLength(300)]
        public string Title { get; set; } = string.Empty;
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;
        public string? Link { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}