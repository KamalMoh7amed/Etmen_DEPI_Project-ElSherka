using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Patient
{
    public class ProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public PhysicalActivityLevel ActivityLevel { get; set; }
        public string? BloodType { get; set; }
        public bool HasChronicDiseases { get; set; }
        public string? ChronicDiseasesNotes { get; set; }
        public string? Allergies { get; set; }
        public string? CurrentMedications { get; set; }
    }
}