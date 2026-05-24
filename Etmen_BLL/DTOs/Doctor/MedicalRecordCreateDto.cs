namespace Etmen_BLL.DTOs.Doctor
{
    public class MedicalRecordCreateDto
    {
        public int PatientId { get; set; }
        public DateTime RecordDate { get; set; }
        public decimal? SystolicBP { get; set; }
        public decimal? DiastolicBP { get; set; }
        public decimal? BloodSugar { get; set; }
        public decimal? HeartRate { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? OxygenSaturation { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public string? Notes { get; set; }
        public List<string>? PrescribedMedications { get; set; }
    }
}