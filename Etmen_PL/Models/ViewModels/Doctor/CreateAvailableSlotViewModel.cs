using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Doctor
{
    public class CreateAvailableSlotViewModel
    {
        [Required(ErrorMessage = "التاريخ مطلوب")]
        [DataType(DataType.Date)]
        public DateTime SlotDate { get; set; }

        [Required(ErrorMessage = "وقت البداية مطلوب")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "وقت النهاية مطلوب")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }
    }
}
