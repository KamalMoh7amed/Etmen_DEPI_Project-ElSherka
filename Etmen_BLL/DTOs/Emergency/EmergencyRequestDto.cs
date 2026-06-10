namespace Etmen_BLL.DTOs.Emergency
{
    public class EmergencyRequestDto
    {
        public int Id { get; set; }
        public int PatientProfileId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? EmergencyType { get; set; }
        public string? Description { get; set; }
    }
}
