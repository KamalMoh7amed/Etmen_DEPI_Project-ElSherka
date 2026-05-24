namespace Etmen_BLL.DTOs.Admin
{
    public class SystemConfigDto
    {
        public bool EnableCrisisMode { get; set; }
        public bool EnableAIChat { get; set; }
        public bool EnableOCR { get; set; }
        public bool EnableFamilyLinking { get; set; }
        public bool EnableEmergencyRequests { get; set; }
        public int MaxLoginAttempts { get; set; }
        public int LockoutDurationMinutes { get; set; }
        public int SessionTimeoutMinutes { get; set; }
    }
}