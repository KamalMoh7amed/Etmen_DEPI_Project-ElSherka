using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etmen_Domain.Entities
{
    public class HealthcareProvider
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        [Column(TypeName = "decimal(9,6)")]
        public decimal Latitude { get; set; }
        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }
        [StringLength(20)]
        public string? Phone { get; set; }
        public int? AvailableBeds { get; set; }
        public bool IsEmergencyCenter { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<DoctorProvider> DoctorProviders { get; set; } = new List<DoctorProvider>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}