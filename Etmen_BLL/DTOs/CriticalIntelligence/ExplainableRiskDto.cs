using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class ExplainableRiskDto
    {
        public decimal RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string PlainLanguageSummary { get; set; } = string.Empty;
        public List<RiskContributionDto> Contributions { get; set; } = new();
        public List<string> ImmediateActions { get; set; } = new();
    }

    public class RiskContributionDto
    {
        public string Factor { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int ImpactPercent { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}
