using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class EmergencyCaseDetailDto
    {
        // Case
        public int EmergencyRequestId { get; set; }
        public EmergencyRequestStatus Status { get; set; }
        public string EmergencyType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PriorityScore { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? DoctorAssignedAt { get; set; }
        public string? EscalationReason { get; set; }
        public bool IsAssignedToCurrentDoctor { get; set; }

        // Risk
        public decimal RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string? Symptoms { get; set; }

        // Patient
        public int PatientProfileId { get; set; }
        public string PatientUserId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public string? PatientEmail { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? BloodType { get; set; }
        public bool HasChronicDiseases { get; set; }
        public string? ChronicDiseasesNotes { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }

        // Latest Vitals
        public decimal? SystolicBP { get; set; }
        public decimal? DiastolicBP { get; set; }
        public decimal? HeartRate { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? OxygenSaturation { get; set; }
        public decimal? BloodSugar { get; set; }
        public DateTime? VitalsRecordedAt { get; set; }

        // Assigned Doctor
        public string? AssignedDoctorName { get; set; }
        public string? AssignedDoctorSpecialization { get; set; }

        // Hospital
        public string? HospitalName { get; set; }

        // Suggested message
        public string SuggestedFirstMessage { get; set; } = string.Empty;

        // Recommendations
        public string? PatientRecommendations { get; set; }
        public string? FamilyRecommendations { get; set; }
        public string? PrescribedMedications { get; set; }

        // Family Members for Quick Chat
        public List<FamilyMemberChatDto> PatientFamilyMembers { get; set; } = new();
    }
}
