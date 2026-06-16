using System.ComponentModel.DataAnnotations;

namespace Etmen_Domain.Entities
{
    public class StaffActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StaffProfileId { get; set; }
        public virtual StaffProfile StaffProfile { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
