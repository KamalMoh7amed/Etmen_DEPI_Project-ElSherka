using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.HospitalStaff
{
    public class HospitalStaffEmergencyDetailDto
    {
        public int RequestId { get; set; }
        public EmergencyRequestStatus Status { get; set; }
        public string EmergencyType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ResponseNotes { get; set; }

        public int PatientProfileId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public string? PatientEmail { get; set; }
        public string? BloodType { get; set; }
        public bool HasChronicDiseases { get; set; }
        public string? ChronicDiseasesNotes { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? AssignedProviderId { get; set; }
        public string? AssignedProviderName { get; set; }
        public string? AssignedProviderPhone { get; set; }
        public int? AssignedProviderAvailableBeds { get; set; }
    }
}
