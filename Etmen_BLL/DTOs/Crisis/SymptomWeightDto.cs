namespace Etmen_BLL.DTOs.Crisis
{
    public class SymptomWeightDto
    {
        public string SymptomName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public bool IsEmergencySymptom { get; set; }
    }
}