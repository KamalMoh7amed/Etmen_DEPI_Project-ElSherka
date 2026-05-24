using System.ComponentModel.DataAnnotations;
using Etmen_Domain.Enums;

namespace Etmen_Domain.Entities
{
    public class CrisisConfiguration
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string CrisisName { get; set; } = string.Empty;
        public CrisisType CrisisType { get; set; }
        public SystemMode SystemMode { get; set; } = SystemMode.Normal;
        public bool IsActive { get; set; } = false;

        [StringLength(1000)]
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal EmergencyThreshold { get; set; } = 0.7m;
        public decimal HighRiskThreshold { get; set; } = 0.5m;
        public decimal MediumRiskThreshold { get; set; } = 0.3m;

        public virtual ICollection<SymptomWeight> SymptomWeights { get; set; } = new List<SymptomWeight>();
        public virtual ICollection<OutbreakZone> OutbreakZones { get; set; } = new List<OutbreakZone>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}