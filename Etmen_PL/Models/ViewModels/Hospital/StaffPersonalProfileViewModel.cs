using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Hospital
{
    public class StaffPersonalProfileViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "كلمة المرور الحالية")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "كلمة المرور الجديدة")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "يجب أن تكون كلمة المرور 6 أحرف على الأقل")]
        public string? NewPassword { get; set; }

        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "كلمة المرور الجديدة وتأكيدها غير متطابقين")]
        public string? ConfirmNewPassword { get; set; }

        // Read-only properties for UI display
        public string ActiveShift { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
    }
}
