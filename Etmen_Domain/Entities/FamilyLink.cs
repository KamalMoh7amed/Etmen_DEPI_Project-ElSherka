using System.ComponentModel.DataAnnotations;

namespace Etmen_Domain.Entities
{
    public class FamilyLink
    {
        [Key]
        public int Id { get; set; }

        public int PrimaryPatientId { get; set; }
        public virtual PatientProfile PrimaryPatient { get; set; } = null!;

        public int LinkedPatientId { get; set; }
        public virtual PatientProfile LinkedPatient { get; set; } = null!;

        [StringLength(100)]
        public string Relationship { get; set; } = string.Empty;
        public string InviteToken { get; set; } = string.Empty;

        public bool IsAccepted { get; set; } = false;
        public bool CanViewRecords { get; set; } = false;
        public bool CanViewRisk { get; set; } = false;
        public bool CanBookAppointments { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
    }
}