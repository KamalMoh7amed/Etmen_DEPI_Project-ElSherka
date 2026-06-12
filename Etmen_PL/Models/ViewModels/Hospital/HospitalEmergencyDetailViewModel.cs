using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Hospital
{
    public class HospitalEmergencyDetailViewModel
    {
        public int RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string EmergencyType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public string? ResponseNotes { get; set; }

        // Patient Information
        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public string? BloodType { get; set; }
        public bool HasChronicDiseases { get; set; }
        public string? ChronicDiseasesNotes { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }

        // Location Information
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Hospital Information
        public int? AssignedProviderAvailableBeds { get; set; }

        // Doctor Assignment
        public string? AssignedDoctorUserId { get; set; }
        public List<Etmen_Domain.Entities.DoctorProfile> AvailableDoctors { get; set; } = new();
    }
}
