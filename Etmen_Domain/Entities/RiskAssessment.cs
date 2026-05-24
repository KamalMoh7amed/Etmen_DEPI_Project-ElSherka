using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Etmen_Domain.Enums;

namespace Etmen_Domain.Entities
{
    public class RiskAssessment
    {
        [Key]
        public int Id { get; set; }
        public int PatientProfileId { get; set; }
        public virtual PatientProfile PatientProfile { get; set; } = null!;

        public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(3,2)")]
        [Range(0, 1)]
        public decimal RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }

        [StringLength(1000)]
        public string? Symptoms { get; set; }
        public string? RecommendationsJson { get; set; }
        public bool IsEmergency { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}