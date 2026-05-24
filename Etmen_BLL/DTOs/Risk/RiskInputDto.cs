namespace Etmen_BLL.DTOs.Risk
{
    public class RiskInputDto
    {
        public string? Symptoms { get; set; }
        public decimal? HeartRate { get; set; }
        public decimal? SystolicBP { get; set; }
        public decimal? DiastolicBP { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? OxygenSaturation { get; set; }
        public decimal? BloodSugar { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}