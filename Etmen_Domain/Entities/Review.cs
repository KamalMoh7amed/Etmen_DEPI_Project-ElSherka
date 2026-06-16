using System;
using System.ComponentModel.DataAnnotations;

namespace Etmen_Domain.Entities
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int PatientProfileId { get; set; }
        public virtual PatientProfile PatientProfile { get; set; } = null!;

        public int? DoctorProfileId { get; set; }
        public virtual DoctorProfile? DoctorProfile { get; set; }

        public int? HealthcareProviderId { get; set; }
        public virtual HealthcareProvider? HealthcareProvider { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
