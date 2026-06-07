namespace Etmen_BLL.DTOs.Auth
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
