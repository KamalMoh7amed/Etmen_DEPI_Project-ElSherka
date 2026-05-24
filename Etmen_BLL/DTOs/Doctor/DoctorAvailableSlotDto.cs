namespace Etmen_BLL.DTOs.Doctor
{
    public class DoctorAvailableSlotDto
    {
        public int Id { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan SlotStart { get; set; }
        public TimeSpan SlotEnd { get; set; }
        public bool IsBooked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}