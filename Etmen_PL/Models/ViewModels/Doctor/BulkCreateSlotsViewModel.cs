using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Doctor
{
    public class BulkCreateSlotsViewModel
    {
        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "وقت البداية مطلوب")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "وقت النهاية مطلوب")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Range(15, 120, ErrorMessage = "مدة الفتحة يجب أن تكون بين 15 و 120 دقيقة")]
        public int SlotDurationMinutes { get; set; } = 30;

        [StringLength(500)]
        public string? DaysOfWeek { get; set; }
    }
}
