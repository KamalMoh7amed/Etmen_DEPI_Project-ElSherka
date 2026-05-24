namespace Etmen_BLL.DTOs.Patient
{
    public class DoctorPatientDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public int? YearsOfExperience { get; set; }
        public decimal? ConsultationFee { get; set; }
        public bool IsAvailable { get; set; }
        public string? Bio { get; set; }
    }
}