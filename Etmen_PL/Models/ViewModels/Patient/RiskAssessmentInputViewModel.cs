using System.ComponentModel.DataAnnotations;

namespace Etmen_PL.Models.ViewModels.Patient
{
    public class RiskAssessmentInputViewModel
    {
        [StringLength(1000)]
        public string? Symptoms { get; set; }

        [Range(20, 300, ErrorMessage = "معدل النبض يجب أن يكون بين 20 و 300")]
        public decimal? HeartRate { get; set; }
 
        [Range(40, 300, ErrorMessage = "ضغط الدم الانقباضي يجب أن يكون بين 40 و 300")]
        public decimal? SystolicBP { get; set; }
 
        [Range(20, 200, ErrorMessage = "ضغط الدم الانبساطي يجب أن يكون بين 20 و 200")]
        public decimal? DiastolicBP { get; set; }
 
        [Range(30, 45, ErrorMessage = "درجة الحرارة يجب أن تكون بين 30 و 45 درجة مئوية")]
        public decimal? Temperature { get; set; }
 
        [Range(30, 100, ErrorMessage = "نسبة الأكسجين يجب أن تكون بين 30% و 100%")]
        public decimal? OxygenSaturation { get; set; }
 
        [Range(20, 600, ErrorMessage = "مستوى السكر يجب أن يكون بين 20 و 600 mg/dL")]
        public decimal? BloodSugar { get; set; }

        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        public decimal? Longitude { get; set; }
    }
}
