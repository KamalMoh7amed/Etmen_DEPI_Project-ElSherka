using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Crisis
{
    public class EditCrisisDto
    {
        public int Id { get; set; }
        public string CrisisName { get; set; } = string.Empty;
        public CrisisType CrisisType { get; set; }
        public SystemMode SystemMode { get; set; }
        public string? Description { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal EmergencyThreshold { get; set; }
        public decimal HighRiskThreshold { get; set; }
        public decimal MediumRiskThreshold { get; set; }
    }
}