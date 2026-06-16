using Etmen_BLL.DTOs.CriticalIntelligence;

namespace Etmen_PL.Models.ViewModels.Emergency
{
    public class DoctorPanicInboxViewModel
    {
        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int TotalCriticalCases { get; set; }
        public int AssignedToDoctor { get; set; }
        public int UnassignedCriticalCases { get; set; }
        public bool IsAvailable { get; set; } = true;
        public List<DoctorPanicInboxItemDto> Items { get; set; } = new();
    }
}
