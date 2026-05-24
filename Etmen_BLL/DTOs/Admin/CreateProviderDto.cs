namespace Etmen_BLL.DTOs.Admin
{
    public class CreateProviderDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? AvailableBeds { get; set; }
        public bool IsEmergencyCenter { get; set; }
    }
}