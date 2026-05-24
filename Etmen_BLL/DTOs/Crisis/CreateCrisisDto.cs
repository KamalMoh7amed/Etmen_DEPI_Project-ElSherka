using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Crisis
{
    public class CreateCrisisDto
    {
        public string CrisisName { get; set; } = string.Empty;
        public CrisisType CrisisType { get; set; }
        public SystemMode SystemMode { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal EmergencyThreshold { get; set; } = 0.7m;
        public decimal HighRiskThreshold { get; set; } = 0.5m;
        public decimal MediumRiskThreshold { get; set; } = 0.3m;
    }
}