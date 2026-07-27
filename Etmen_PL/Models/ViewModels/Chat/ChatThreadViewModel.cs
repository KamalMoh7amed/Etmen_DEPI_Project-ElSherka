using Etmen_BLL.DTOs.Chat;
using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Chat
{
    public class ChatThreadViewModel
    {
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public IEnumerable<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();

        [Required(ErrorMessage = "الرسالة مطلوبة")]
        [StringLength(2000)]
        public string? MessageContent { get; set; }
    }
}
