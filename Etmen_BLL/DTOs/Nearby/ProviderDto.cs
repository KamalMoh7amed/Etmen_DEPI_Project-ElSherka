namespace Etmen_BLL.DTOs.Nearby
{
    public class ProviderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? AvailableBeds { get; set; }
        public bool IsEmergencyCenter { get; set; }
        public decimal DistanceKm { get; set; }
        public object Latitude { get; internal set; }
        public object Longitude { get; internal set; }
    }
}