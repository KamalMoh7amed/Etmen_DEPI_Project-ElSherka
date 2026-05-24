using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etmen_Domain.Entities
{
    public class MedicalRecord
    {
        [Key]
        public int Id { get; set; }
        public int PatientProfileId { get; set; }
        public virtual PatientProfile PatientProfile { get; set; } = null!;

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? SystolicBP { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiastolicBP { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? BloodSugar { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? HeartRate { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Temperature { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? OxygenSaturation { get; set; }

        [StringLength(1000)]
        public string? Symptoms { get; set; }
        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}