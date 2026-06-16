using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class DoctorPanicInboxItemDto
    {
        public int EmergencyRequestId { get; set; }
        public int PatientProfileId { get; set; }
        public string PatientUserId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public decimal RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string? Symptoms { get; set; }
        public int PriorityScore { get; set; }
        public bool IsAssignedToCurrentDoctor { get; set; }
        public bool HasConversation { get; set; }
        public DateTime RequestedAt { get; set; }
        public string SuggestedFirstMessage { get; set; } = string.Empty;
    }
}
