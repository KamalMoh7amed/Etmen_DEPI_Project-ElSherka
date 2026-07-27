using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Doctor
{
    public class DoctorProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Specialization { get; set; }

        [StringLength(50)]
        public string? LicenseNumber { get; set; }

        [Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [Range(0, 10000)]
        public decimal? ConsultationFee { get; set; }

        public bool IsAvailable { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
