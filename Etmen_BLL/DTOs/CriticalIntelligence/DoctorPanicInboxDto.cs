namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class DoctorPanicInboxDto
    {
        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int TotalCriticalCases { get; set; }
        public int AssignedToDoctor { get; set; }
        public int UnassignedCriticalCases { get; set; }
        public bool IsAvailable { get; set; }
        public List<DoctorPanicInboxItemDto> Items { get; set; } = new();
    }
}
