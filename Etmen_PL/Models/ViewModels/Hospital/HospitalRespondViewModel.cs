using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Hospital
{
    public class HospitalRespondViewModel
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Required(ErrorMessage = "الحالة مطلوبة")]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // Accepted, Rejected, Completed

        [StringLength(1000)]
        public string? ResponseNotes { get; set; }

        public string? AssignedDoctorUserId { get; set; }
    }
}
