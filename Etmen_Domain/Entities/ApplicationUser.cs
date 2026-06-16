using Microsoft.AspNetCore.Identity;

namespace Etmen_Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public DateTime? EmailVerificationSentAt { get; set; }
        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpiry { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public bool MustChangePassword { get; set; } = false;

        public virtual PatientProfile? PatientProfile { get; set; }
        public virtual DoctorProfile? DoctorProfile { get; set; }
        public virtual StaffProfile? StaffProfile { get; set; }
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
    }
}