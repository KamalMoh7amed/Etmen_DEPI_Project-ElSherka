using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etmen_Domain.Entities
{
    [Owned]
    public class SymptomWeight
    {
        [StringLength(200)]
        public string SymptomName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(3,2)")]
        [Range(0, 1)]
        public decimal Weight { get; set; }

        public bool IsEmergencySymptom { get; set; } = false;
    }
}