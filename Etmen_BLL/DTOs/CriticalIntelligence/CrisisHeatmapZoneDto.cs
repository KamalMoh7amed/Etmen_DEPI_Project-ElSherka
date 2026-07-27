using System.Collections.Generic;

namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class CrisisHeatmapZoneDto
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public decimal CenterLatitude { get; set; }
        public decimal CenterLongitude { get; set; }
        public decimal RadiusInKm { get; set; }
        public int RiskLevel { get; set; }
        public int CriticalCasesInside { get; set; }
        public string SupportStatus { get; set; } = "مشبعة"; // "محتاجة مساعدة", "مشبعة", "تحتاج دكاترة"
        public int BedsNeeded { get; set; }
        public int DoctorsNeeded { get; set; }
        public List<string> NeedingSupplyAreas { get; set; } = new();
    }
}
