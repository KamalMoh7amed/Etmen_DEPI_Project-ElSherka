using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class CriticalCommandCenterDto
    {
        public int ActiveCriticalCases { get; set; }
        public int WaitingForHospital { get; set; }
        public int HospitalAccepted { get; set; }
        public int WaitingForDoctor { get; set; }
        public int DoctorAssigned { get; set; }
        public decimal AverageWaitingMinutes { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<CriticalCommandCenterItemDto> Cases { get; set; } = new();
    }

    public class CriticalCommandCenterItemDto
    {
        public int EmergencyRequestId { get; set; }
        public int PatientProfileId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public decimal RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string? Symptoms { get; set; }
        public EmergencyRequestStatus EmergencyStatus { get; set; }
        public int PriorityScore { get; set; }
        public int WaitingMinutes { get; set; }
        public bool HospitalResponded { get; set; }
        public string? AssignedProviderName { get; set; }
        public string? AssignedDoctorUserId { get; set; }
        public string? AssignedDoctorName { get; set; }
        public DateTime? DoctorAssignedAt { get; set; }
        public bool HasDoctorConversation { get; set; }
        public DateTime? LastDoctorMessageAt { get; set; }
        public string OperationalStatus { get; set; } = string.Empty;
    }
}
