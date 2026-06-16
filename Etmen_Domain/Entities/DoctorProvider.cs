using System.ComponentModel.DataAnnotations.Schema;

namespace Etmen_Domain.Entities
{
    public class DoctorProvider
    {
        public int DoctorProfileId { get; set; }
        public virtual DoctorProfile DoctorProfile { get; set; } = null!;

        public int HealthcareProviderId { get; set; }
        public virtual HealthcareProvider HealthcareProvider { get; set; } = null!;

        public bool IsEmergencyDoctor { get; set; } = false;
        public bool IsOwner { get; set; } = false; // true if it's the doctor's private clinic
        
        [Column(TypeName = "varchar(100)")]
        public string? AffiliationRole { get; set; } // e.g., "Consultant", "ER Doctor"
    }
}
