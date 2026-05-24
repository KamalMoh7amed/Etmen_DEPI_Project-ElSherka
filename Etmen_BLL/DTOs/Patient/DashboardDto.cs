using Etmen_BLL.DTOs.Risk;

namespace Etmen_BLL.DTOs.Patient
{
    public class DashboardDto
    {
        public string PatientName { get; set; } = string.Empty;
        public RiskResultDto? LatestRiskAssessment { get; set; }
        public int UnreadAlertsCount { get; set; }
        public int UpcomingAppointmentsCount { get; set; }
        public decimal? LatestBmi { get; set; }
        public string? LatestBmiCategory { get; set; }
        public List<RecentAppointmentDto> UpcomingAppointments { get; set; } = new List<RecentAppointmentDto>();
        public List<RecentAlertDto> RecentAlerts { get; set; } = new List<RecentAlertDto>();
    }
}