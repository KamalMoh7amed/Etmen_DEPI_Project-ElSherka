using Etmen_BLL.DTOs.Patient;
using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.DTOs.Risk;

namespace Etmen_PL.Models.ViewModels.Patient
{
    /// <summary>
    /// ViewModel for Patient Dashboard
    /// Maps from DashboardDto in BLL
    /// </summary>
    public class PatientDashboardViewModel
    {
        public string PatientName { get; set; } = string.Empty;
        public RiskResultDto? LatestRiskAssessment { get; set; }
        public int UnreadAlertsCount { get; set; }
        public int UpcomingAppointmentsCount { get; set; }
        public decimal? LatestBmi { get; set; }
        public string? LatestBmiCategory { get; set; }
        public int MedicalRecordsCount { get; set; }
        public MedicalRecordDto? LatestMedicalRecord { get; set; }
        public List<RecentAppointmentDto> UpcomingAppointments { get; set; } = new();
        public List<RecentAlertDto> RecentAlerts { get; set; } = new();

        // Active Emergency Case Details & Recommendations
        public bool HasActiveEmergency { get; set; }
        public int ActiveEmergencyId { get; set; }
        public string? ActiveEmergencyDoctorName { get; set; }
        public string? ActiveEmergencyDoctorUserId { get; set; }
        public string? ActiveEmergencyHospitalName { get; set; }
        public string? ActiveEmergencyPatientRecommendations { get; set; }
        public string? ActiveEmergencyFamilyRecommendations { get; set; }
        public string? ActiveEmergencyPrescribedMedications { get; set; }
    }
}
