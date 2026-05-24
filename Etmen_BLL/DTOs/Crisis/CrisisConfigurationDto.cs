using Etmen_Domain.Enums;

namespace Etmen_BLL.DTOs.Crisis
{
    public class CrisisConfigurationDto
    {
        public int Id { get; set; }
        public string CrisisName { get; set; } = string.Empty;
        public CrisisType CrisisType { get; set; }
        public SystemMode SystemMode { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<SymptomWeightDto> SymptomWeights { get; set; } = new List<SymptomWeightDto>();
        public int ZonesCount { get; set; }
    }
}