using System;

namespace Etmen_BLL.DTOs.Patient
{
    public class RecentAppointmentDto
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}