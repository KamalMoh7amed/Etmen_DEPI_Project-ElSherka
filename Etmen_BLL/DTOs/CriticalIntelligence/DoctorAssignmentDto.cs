namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class DoctorAssignmentDto
    {
        public int EmergencyRequestId { get; set; }
        public string DoctorUserId { get; set; } = string.Empty;
        public int DoctorProfileId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public int MatchScore { get; set; }
        public DateTime AssignedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
