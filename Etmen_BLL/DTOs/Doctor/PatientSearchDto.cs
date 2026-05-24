namespace Etmen_BLL.DTOs.Doctor
{
    public class PatientSearchDto
    {
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? FilterBy { get; set; } // "Name", "Phone", "LastVisit"
    }
}