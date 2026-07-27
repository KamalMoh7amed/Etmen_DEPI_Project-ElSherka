using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Hospital
{
    public class HospitalBedsUpdateViewModel
    {
        [Required]
        public int ProviderId { get; set; }

        [Required(ErrorMessage = "عدد الأسرة المتاحة مطلوب")]
        [Range(0, 1000)]
        public int AvailableBeds { get; set; }
    }
}
