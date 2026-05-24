using System.ComponentModel.DataAnnotations;

namespace Etmen_Domain.Entities
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public string SenderId { get; set; } = string.Empty;
        public virtual ApplicationUser Sender { get; set; } = null!;

        public string ReceiverId { get; set; } = string.Empty;
        public virtual ApplicationUser Receiver { get; set; } = null!;

        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}