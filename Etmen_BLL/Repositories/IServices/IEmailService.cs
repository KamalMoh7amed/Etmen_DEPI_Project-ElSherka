namespace Etmen_BLL.Repositories.IServices
{
    
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);

        Task SendEmailWithPdfAttachmentAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            byte[] pdfBytes,
            string pdfFileName);

        // ── Specific transactional emails ────────────────────────────────────

        /// <summary>Sends account-activation email (verification link).</summary>
        Task SendAccountActivationEmailAsync(string toEmail, string toName, string activationLink, string role);

        /// <summary>Sends welcome email after email is confirmed.</summary>
        Task SendWelcomeEmailAsync(string toEmail, string toName, string role);

        /// <summary>Sends lab-result report with a PDF attachment.</summary>
        Task SendLabResultEmailAsync(string toEmail, string toName, string testName, DateTime testDate, byte[] reportPdf);

        /// <summary>Sends appointment-booking confirmation to patient and doctor.</summary>
        Task SendAppointmentConfirmationEmailAsync(
            string toEmail, string toName,
            string doctorName, string patientName,
            DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime,
            string? notes, bool isDoctor, byte[]? appointmentPdf = null);

        /// <summary>Sends appointment-reminder email (1 day before or 2 hours before).</summary>
        Task SendAppointmentReminderEmailAsync(
            string toEmail, string toName,
            string doctorName, string patientName,
            DateTime appointmentDate, TimeSpan startTime,
            string timeRemainingLabel);

        /// <summary>Sends appointment-cancellation notification.</summary>
        Task SendAppointmentCancellationEmailAsync(
            string toEmail, string toName,
            string doctorName, string patientName,
            DateTime appointmentDate, TimeSpan startTime,
            bool isDoctor);

        /// <summary>Sends risk-alert email to the patient or a family member.</summary>
        Task SendRiskAlertEmailAsync(
            string toEmail, string toName,
            string patientName, string riskLevel, decimal riskScore,
            List<string> recommendations, bool isFamilyMember, byte[]? riskReportPdf = null);

        /// <summary>Sends password-reset email with a reset link.</summary>
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);

        /// <summary>Sends emergency-request confirmation email.</summary>
        Task SendEmergencyConfirmationEmailAsync(string toEmail, string toName, string emergencyType, DateTime requestTime);

        /// <summary>Sends doctor approval / rejection notification.</summary>
        Task SendDoctorApprovalEmailAsync(string toEmail, string toName, bool isApproved, string? reason = null);
    }
}
