using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Etmen_Domain.Enums;

namespace Etmen_Domain.Entities
{
    public class EmergencyRequest
    {
        [Key]
        public int Id { get; set; }

        public int PatientProfileId { get; set; }
        public virtual PatientProfile PatientProfile { get; set; } = null!;

        public int? HealthcareProviderId { get; set; }
        public virtual HealthcareProvider? HealthcareProvider { get; set; }

        public EmergencyRequestStatus Status { get; set; } = EmergencyRequestStatus.Pending;

        [StringLength(500)]
        public string? EmergencyType { get; set; }
        [StringLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(9,6)")]
        public decimal? Longitude { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [StringLength(500)]
        public string? ResponseNotes { get; set; }
    }
}