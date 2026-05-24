namespace Etmen_BLL.DTOs.Medical
{
    public class MedicalRecordDto
    {
        public int Id { get; set; }
        public DateTime RecordDate { get; set; }
        public decimal? SystolicBP { get; set; }
        public decimal? DiastolicBP { get; set; }
        public decimal? BloodSugar { get; set; }
        public decimal? HeartRate { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? OxygenSaturation { get; set; }
        public string? Symptoms { get; set; }
        public string? Notes { get; set; }
    }
}