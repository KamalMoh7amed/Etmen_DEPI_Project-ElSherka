namespace Etmen_BLL.DTOs.Doctor
{
    public class UpdateAppointmentStatusDto
    {
        public int AppointmentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}