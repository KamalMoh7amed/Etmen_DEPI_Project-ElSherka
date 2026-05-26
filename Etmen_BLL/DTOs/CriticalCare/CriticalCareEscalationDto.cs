using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.CriticalCare
{
    public class CriticalCareEscalationDto
    {
        public bool WasEscalated { get; set; }
        public bool WasAlreadyEscalated { get; set; }
        public int? EmergencyRequestId { get; set; }
        public int PriorityScore { get; set; }
        public EmergencyRequestStatus? EmergencyStatus { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
