namespace Etmen_BLL.DTOs.CriticalIntelligence
{
    public class AiMedicalSummaryDto
    {
        public int PatientProfileId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> CriticalFindings { get; set; } = new();
        public List<string> SuggestedDoctorQuestions { get; set; } = new();
        public List<string> MissingInformation { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
