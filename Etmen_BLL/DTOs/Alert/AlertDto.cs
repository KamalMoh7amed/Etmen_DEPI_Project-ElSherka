using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Alert
{
    public class AlertDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertStatus Status { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
