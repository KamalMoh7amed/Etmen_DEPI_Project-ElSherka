using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Doctor
{
    public class UpdateAppointmentStatusViewModel
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "حالة التعيين مطلوبة")]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
