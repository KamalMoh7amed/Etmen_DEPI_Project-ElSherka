using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Admin
{
    public class CreateCrisisViewModel
    {
        [Required(ErrorMessage = "اسم الأزمة مطلوب")]
        [StringLength(200)]
        public string CrisisName { get; set; } = string.Empty;

        [StringLength(50)]
        public string CrisisType { get; set; } = string.Empty; // Epidemic, Pandemic, etc.

        [StringLength(50)]
        public string SystemMode { get; set; } = "Normal";

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(0, 1)]
        public decimal EmergencyThreshold { get; set; } = 0.8m;

        [Range(0, 1)]
        public decimal HighRiskThreshold { get; set; } = 0.6m;

        [Range(0, 1)]
        public decimal MediumRiskThreshold { get; set; } = 0.4m;
    }
}
