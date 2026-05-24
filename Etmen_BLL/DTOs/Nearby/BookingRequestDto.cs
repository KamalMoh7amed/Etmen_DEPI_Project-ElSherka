namespace Etmen_BLL.DTOs.Nearby
{
    public class BookingRequestDto
    {
        public int DoctorId { get; set; }
        public int SlotId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Notes { get; set; }
    }
}   