using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Admin
{
    public class UpdateUserStatusViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "الحالة مطلوبة")]
        public bool IsActive { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }
}
