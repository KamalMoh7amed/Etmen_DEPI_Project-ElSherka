namespace Etmen_BLL.DTOs.Doctor
{
    public class DoctorStatisticsDto
    {
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int NoShowAppointments { get; set; }
        public decimal CompletionRate { get; set; }
        public int TotalPatients { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public decimal? AverageConsultationFee { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}