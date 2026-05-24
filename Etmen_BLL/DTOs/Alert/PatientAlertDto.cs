using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Alert
{
    public class PatientAlertDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertStatus Status { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}