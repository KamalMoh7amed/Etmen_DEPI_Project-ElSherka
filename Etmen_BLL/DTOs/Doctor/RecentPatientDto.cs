namespace Etmen_BLL.DTOs.Doctor
{
    public class RecentPatientDto
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime? LastVisitDate { get; set; }
        public string? LastDiagnosis { get; set; }
        public int TotalVisits { get; set; }
    }
}