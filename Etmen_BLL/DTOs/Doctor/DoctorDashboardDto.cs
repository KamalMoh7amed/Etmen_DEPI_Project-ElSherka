namespace Etmen_BLL.DTOs.Doctor
{
    public class DoctorDashboardDto
    {
        public string DoctorName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public int TodayAppointmentsCount { get; set; }
        public int PendingAppointmentsCount { get; set; }
        public int TotalPatientsCount { get; set; }
        public decimal? AverageRating { get; set; }
        public List<UpcomingAppointmentDto> UpcomingAppointments { get; set; } = new List<UpcomingAppointmentDto>();
        public List<RecentPatientDto> RecentPatients { get; set; } = new List<RecentPatientDto>();
    }
}