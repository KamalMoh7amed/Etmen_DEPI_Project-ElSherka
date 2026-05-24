namespace Etmen_BLL.DTOs.Admin
{
    public class BulkUserActionDto
    {
        public List<string> UserIds { get; set; } = new List<string>();
        public string Action { get; set; } = string.Empty; // Activate, Deactivate, ChangeRole, Delete
        public string? NewRole { get; set; }
        public string? Reason { get; set; }
    }
}