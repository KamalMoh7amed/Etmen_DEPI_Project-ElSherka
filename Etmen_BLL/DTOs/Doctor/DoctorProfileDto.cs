namespace Etmen_BLL.DTOs.Doctor
{
    public class DoctorProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }
        public decimal? ConsultationFee { get; set; }
        public bool IsAvailable { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}