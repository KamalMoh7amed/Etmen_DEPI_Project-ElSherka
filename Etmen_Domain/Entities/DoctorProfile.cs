using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etmen_Domain.Entities
{
    public class DoctorProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        [StringLength(200)]
        public string? FullName { get; set; }
        [StringLength(300)]
        public string? Specialization { get; set; }
        [StringLength(100)]
        public string? LicenseNumber { get; set; }
        public int? YearsOfExperience { get; set; }
        [StringLength(500)]
        public string? Bio { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? ConsultationFee { get; set; }
        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<AvailableSlot> AvailableSlots { get; set; } = new List<AvailableSlot>();
    }
}