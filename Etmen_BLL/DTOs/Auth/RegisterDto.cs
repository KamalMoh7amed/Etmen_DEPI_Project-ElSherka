using System.ComponentModel.DataAnnotations;

namespace Etmen_BLL.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "الاسم الأول يجب أن يكون بين 2 و 100 حرف")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم العائلة يجب أن يكون بين 2 و 100 حرف")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [Display(Name = "رقم الهاتف")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// دور المستخدم: "Patient" أو "Doctor"
        /// </summary>
        [Required(ErrorMessage = "يجب اختيار نوع الحساب")]
        public string Role { get; set; } = "Patient";
    }
}
