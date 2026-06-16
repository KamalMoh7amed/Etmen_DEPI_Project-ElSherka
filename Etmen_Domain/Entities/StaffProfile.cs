using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Etmen_Domain.Enums;

namespace Etmen_Domain.Entities
{
    public class StaffProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        [Required]
        public int HealthcareProviderId { get; set; }
        public virtual HealthcareProvider HealthcareProvider { get; set; } = null!;

        public StaffRoleType RoleType { get; set; } = StaffRoleType.Receptionist;
        public StaffShiftType ActiveShift { get; set; } = StaffShiftType.None;

        public bool IsInvitationAccepted { get; set; } = false;
        
        [StringLength(450)]
        public string? InvitationSenderUserId { get; set; }
        
        [StringLength(200)]
        public string? InvitationToken { get; set; }
        
        public DateTime? InvitationTokenExpiry { get; set; }
        public DateTime? JoinedAt { get; set; }
    }
}
