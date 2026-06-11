using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Risk
{
    public class RiskResultDto
    {
        public int Id { get; set; }
        public decimal RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string RiskColor { get; set; } = string.Empty;
        public string RiskLabel { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public List<string> TriggeredSymptoms { get; set; } = new List<string>();
        public string? NearestEmergencyCenter { get; set; }
        public bool WasAutoEscalated { get; set; }
        public int? EmergencyRequestId { get; set; }
        public string? EscalationMessage { get; set; }
        public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
    }
}
