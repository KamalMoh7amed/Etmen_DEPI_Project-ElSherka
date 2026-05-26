namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class CrisisHeatmapDto
    {
        public int? CrisisId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalGeoTaggedCriticalCases { get; set; }
        public List<CrisisHeatmapPointDto> Points { get; set; } = new();
        public List<CrisisHeatmapZoneDto> Zones { get; set; } = new();
    }

    public class CrisisHeatmapPointDto
    {
        public int EmergencyRequestId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int Intensity { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class CrisisHeatmapZoneDto
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public decimal CenterLatitude { get; set; }
        public decimal CenterLongitude { get; set; }
        public decimal RadiusInKm { get; set; }
        public int RiskLevel { get; set; }
        public int CriticalCasesInside { get; set; }
    }
}
