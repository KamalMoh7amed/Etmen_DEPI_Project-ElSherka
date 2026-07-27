using Etmen_Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Patient
{
    public class PatientProfileViewModel
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [Range(50, 250, ErrorMessage = "الارتفاع يجب أن يكون بين 50 و 250 سم")]
        public decimal? Height { get; set; }

        [Range(10, 500, ErrorMessage = "الوزن يجب أن يكون بين 10 و 500 كغ")]
        public decimal? Weight { get; set; }

        public PhysicalActivityLevel ActivityLevel { get; set; }

        [StringLength(10)]
        public string? BloodType { get; set; }

        public bool HasChronicDiseases { get; set; }

        [StringLength(500)]
        public string? ChronicDiseasesNotes { get; set; }

        [StringLength(500)]
        public string? Allergies { get; set; }

        [StringLength(500)]
        public string? CurrentMedications { get; set; }
    }
}
