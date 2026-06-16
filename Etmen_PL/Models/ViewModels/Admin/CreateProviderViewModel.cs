using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Admin
{
    public class CreateProviderViewModel
    {
        [Required(ErrorMessage = "اسم المركز الصحي مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ProviderType { get; set; } // Hospital, Clinic

        [StringLength(500)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "خط العرض مطلوب")]
        [Range(-90, 90)]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "خط الطول مطلوب")]
        [Range(-180, 180)]
        public decimal Longitude { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        [Range(0, 1000)]
        public int? AvailableBeds { get; set; }

        public bool IsEmergencyCenter { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
