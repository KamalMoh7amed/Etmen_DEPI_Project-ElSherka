namespace Etmen_BLL.DTOs.Emergency
{
    public class EmergencyUpdateDto
    {
        public int RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ResponseNotes { get; set; }
        public int? AssignedProviderId { get; set; }
    }
}