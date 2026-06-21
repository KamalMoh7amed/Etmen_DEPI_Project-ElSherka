
namespace Etmen_BLL.DTOs.Admin
{
    public class AdminCrisisDto
    {
        public int Id { get; set; }
        public string CrisisName { get; set; } = string.Empty;
        public CrisisType CrisisType { get; set; }
        public SystemMode SystemMode { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int ZonesCount { get; set; }
    }
}