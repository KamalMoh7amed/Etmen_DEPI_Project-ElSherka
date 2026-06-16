using System;

namespace Etmen_BLL.DTOs.Review
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int PatientProfileId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int? DoctorProfileId { get; set; }
        public int? HealthcareProviderId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
