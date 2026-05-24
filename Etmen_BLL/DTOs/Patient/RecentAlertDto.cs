using System;

namespace Etmen_BLL.DTOs.Patient
{
    public class RecentAlertDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}