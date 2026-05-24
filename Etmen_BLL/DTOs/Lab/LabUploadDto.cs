namespace Etmen_BLL.DTOs.Lab
{
    public class LabUploadDto
    {
        public string TestName { get; set; } = string.Empty;
        public DateTime TestDate { get; set; }
        public string? FilePath { get; set; }
        public bool UseOcr { get; set; } = true;
    }
}