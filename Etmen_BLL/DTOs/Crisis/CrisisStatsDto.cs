namespace Etmen_BLL.DTOs.Crisis
{
    public class CrisisStatsDto
    {
        public int TotalAssessments { get; set; }
        public int HighRiskCount { get; set; }
        public int CriticalCount { get; set; }
        public int OutbreakZonesCount { get; set; }
        public decimal AverageRiskScore { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}