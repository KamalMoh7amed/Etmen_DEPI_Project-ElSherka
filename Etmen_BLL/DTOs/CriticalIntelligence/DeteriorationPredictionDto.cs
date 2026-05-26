using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class DeteriorationPredictionDto
    {
        public int PatientProfileId { get; set; }
        public decimal Probability { get; set; }
        public RiskLevel PredictedRiskLevel { get; set; }
        public int HoursWindow { get; set; } = 24;
        public string Trend { get; set; } = string.Empty;
        public List<string> Reasons { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
    }
}
