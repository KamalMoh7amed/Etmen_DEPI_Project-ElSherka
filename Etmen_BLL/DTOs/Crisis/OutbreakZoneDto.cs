namespace Etmen_BLL.DTOs.Crisis
{
    public class OutbreakZoneDto
    {
        public string ZoneName { get; set; } = string.Empty;
        public decimal CenterLatitude { get; set; }
        public decimal CenterLongitude { get; set; }
        public decimal RadiusInKm { get; set; }
        public string? PolygonCoordinatesJson { get; set; }
        public int RiskLevel { get; set; }
    }
}