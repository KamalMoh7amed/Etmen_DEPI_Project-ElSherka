using System.ComponentModel.DataAnnotations;

namespace Etmen_Domain.Entities
{
    public class LabResult
    {
        [Key]
        public int Id { get; set; }

        public int PatientProfileId { get; set; }
        public virtual PatientProfile PatientProfile { get; set; } = null!;

        [StringLength(300)]
        public string TestName { get; set; } = string.Empty;
        public DateTime TestDate { get; set; }

        [StringLength(500)]
        public string? FilePath { get; set; }
        [StringLength(500)]
        public string? FileUrl { get; set; }
        public string? OcrExtractedData { get; set; }
        [StringLength(1000)]
        public string? Results { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}