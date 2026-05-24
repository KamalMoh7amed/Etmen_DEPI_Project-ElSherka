using System.ComponentModel.DataAnnotations;

namespace Etmen_Domain.Entities
{
    public class AvailableSlot
    {
        [Key]
        public int Id { get; set; }

        public int DoctorProfileId { get; set; }
        public virtual DoctorProfile DoctorProfile { get; set; } = null!;

        public DateTime SlotDate { get; set; }
        public TimeSpan SlotStart { get; set; }
        public TimeSpan SlotEnd { get; set; }

        public bool IsBooked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}