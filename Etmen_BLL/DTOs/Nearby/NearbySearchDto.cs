namespace Etmen_BLL.DTOs.Nearby
{
    public class NearbySearchDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Type { get; set; } = string.Empty;
        public int RadiusInKm { get; set; } = 10;
    }
}