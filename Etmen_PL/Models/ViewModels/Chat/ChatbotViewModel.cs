using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Chat
{
    public class ChatbotViewModel
    {
        [Required(ErrorMessage = "السؤال مطلوب")]
        [StringLength(2000)]
        public string? Question { get; set; }

        public string? Response { get; set; }
        public DateTime? ResponseTime { get; set; }
    }
}
