namespace Etmen_BLL.DTOs.Doctor
{
    public class BulkCreateSlotsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan DailyStartTime { get; set; }
        public TimeSpan DailyEndTime { get; set; }
        public int SlotDurationMinutes { get; set; } = 30;
        public List<DayOfWeek> ExcludedDays { get; set; } = new List<DayOfWeek> { DayOfWeek.Friday };
    }
}