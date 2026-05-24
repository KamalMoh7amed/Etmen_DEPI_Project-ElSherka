using Etmen_BLL.DTOs.Risk;
using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Crisis
{
    public class CrisisRiskResultDto
    {
        public decimal RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public bool IsInOutbreakZone { get; set; }
        public string? ZoneName { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public SystemMode SystemMode { get; set; }

        public RiskResultDto ToRiskResultDto() => new RiskResultDto
        {
            RiskScore = RiskScore,
            RiskLevel = RiskLevel,
            Recommendations = Recommendations,
            IsEmergency = RiskScore >= 0.7m
        };
    }
}