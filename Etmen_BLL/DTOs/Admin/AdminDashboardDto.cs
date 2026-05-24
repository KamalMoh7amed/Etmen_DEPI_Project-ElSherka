namespace Etmen_BLL.DTOs.Admin
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int ActiveDoctors { get; set; }
        public int ActivePatients { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingEmergencyRequests { get; set; }
        public bool IsCrisisModeActive { get; set; }
        public string? ActiveCrisisName { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}