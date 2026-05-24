using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Etmen_Domain.Enums;

namespace Etmen_Domain.Entities
{
    public class PatientProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        [StringLength(100)]
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(10)]
        public string? Gender { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Height { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [NotMapped]
        public decimal BMI => CalculateBMI();

        public PhysicalActivityLevel ActivityLevel { get; set; } = PhysicalActivityLevel.Sedentary;
        [StringLength(20)]
        public string? BloodType { get; set; }
        public bool HasChronicDiseases { get; set; }
        [StringLength(500)]
        public string? ChronicDiseasesNotes { get; set; }
        [StringLength(500)]
        public string? Allergies { get; set; }
        [StringLength(500)]
        public string? CurrentMedications { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<RiskAssessment> RiskAssessments { get; set; } = new List<RiskAssessment>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
        public virtual ICollection<FamilyLink> PrimaryLinks { get; set; } = new List<FamilyLink>();
        public virtual ICollection<FamilyLink> LinkedLinks { get; set; } = new List<FamilyLink>();
        public virtual ICollection<EmergencyRequest> EmergencyRequests { get; set; } = new List<EmergencyRequest>();

        private decimal CalculateBMI()
        {
            if (Height == null || Weight == null || Height == 0) return 0;
            var h = Height.Value / 100;
            return Math.Round(Weight.Value / (h * h), 2);
        }
    }
}