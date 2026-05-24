namespace Etmen_BLL.DTOs.Admin
{
    public class AdminReportDto
    {
        public string ReportType { get; set; } = string.Empty; // Users, Appointments, Emergencies, Crisis
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRecords { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}