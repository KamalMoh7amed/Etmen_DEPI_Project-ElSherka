using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Crisis
{
    public class CrisisConfigViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الأزمة مطلوب")]
        [StringLength(200)]
        public string CrisisName { get; set; } = string.Empty;

        [StringLength(50)]
        public string CrisisType { get; set; } = string.Empty;

        [StringLength(50)]
        public string SystemMode { get; set; } = "Normal";

        public bool IsActive { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        public List<SymptomWeightViewModel> SymptomWeights { get; set; } = new();

        public int ZonesCount { get; set; }
    }

    public class SymptomWeightViewModel
    {
        [Required(ErrorMessage = "اسم الأعراض مطلوب")]
        [StringLength(100)]
        public string SymptomName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الوزن مطلوب")]
        [Range(0, 1)]
        public decimal Weight { get; set; }

        public bool IsEmergencySymptom { get; set; }
    }
}
