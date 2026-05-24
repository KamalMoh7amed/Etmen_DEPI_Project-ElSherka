namespace Etmen_BLL.DTOs.Admin
{
    public class UpdateUserStatusDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }
}