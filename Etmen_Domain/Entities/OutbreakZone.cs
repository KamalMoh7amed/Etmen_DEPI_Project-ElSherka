using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etmen_Domain.Entities
{
    public class OutbreakZone
    {
        [Key]
        public int Id { get; set; }

        public int CrisisConfigurationId { get; set; }
        public virtual CrisisConfiguration CrisisConfiguration { get; set; } = null!;

        [StringLength(200)]
        public string ZoneName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(9,6)")]
        public decimal CenterLatitude { get; set; }
        [Column(TypeName = "decimal(9,6)")]
        public decimal CenterLongitude { get; set; }

        public decimal RadiusInKm { get; set; }
        public string? PolygonCoordinatesJson { get; set; }
        public int RiskLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}