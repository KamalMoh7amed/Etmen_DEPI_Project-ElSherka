using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Patient
{
    public class LabUploadViewModel
    {
        public int PatientId { get; set; }

        [Required(ErrorMessage = "اسم الاختبار مطلوب")]
        [StringLength(200)]
        public string TestName { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الاختبار مطلوب")]
        [DataType(DataType.Date)]
        public DateTime TestDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? FilePath { get; set; }

        public bool UseOcr { get; set; }

        [Required(ErrorMessage = "يجب تحميل الملف")]
        public IFormFile? LabFile { get; set; }
    }
}
