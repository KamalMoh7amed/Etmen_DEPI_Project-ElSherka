namespace Etmen_BLL.DTOs.Lab
{
    public class LabResultDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public DateTime TestDate { get; set; }
        public string? FilePath { get; set; }
        public string? FileUrl { get; set; }
        public string? OcrExtractedData { get; set; }
        public string? Results { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}