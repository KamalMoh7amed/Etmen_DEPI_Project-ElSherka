namespace Etmen_BLL.DTOs.Doctor
{
    public class CreateAvailableSlotDto
    {
        public DateTime SlotDate { get; set; }
        public TimeSpan SlotStart { get; set; }
        public TimeSpan SlotEnd { get; set; }
    }
}