namespace Etmen_BLL.DTOs.HospitalStaff
{
    public class HospitalStaffEmergencyRespondDto
    {
        public int RequestId { get; set; }
        public int ProviderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ResponseNotes { get; set; }
    }
}
