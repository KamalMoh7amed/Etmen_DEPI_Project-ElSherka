using System.ComponentModel.DataAnnotations;
using Etmen_Domain.Enums;

namespace Etmen_Domain.Entities
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        public int PatientProfileId { get; set; }
        public virtual PatientProfile PatientProfile { get; set; } = null!;

        public int? DoctorProfileId { get; set; }
        public virtual DoctorProfile? DoctorProfile { get; set; }

        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ── Email reminder tracking ──────────────────────────────────────
        /// <summary>True after the "24-hour-before" reminder email has been sent.</summary>
        public bool ReminderSentOneDayBefore { get; set; } = false;

        /// <summary>True after the "2-hour-before" reminder email has been sent.</summary>
        public bool ReminderSentTwoHoursBefore { get; set; } = false;
    }
}