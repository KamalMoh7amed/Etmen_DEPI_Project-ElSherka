
namespace Etmen_BLL.Repositories.Services
{
    /// <summary>
    /// SMTP-based email service that reads settings from appsettings.json ("Email" section).
    /// Handles all transactional emails for the Etmen platform.
    /// </summary>
    public sealed class EmailService : IServices.IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        // Settings pulled once from config
        private readonly string _host;
        private readonly int _port;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;

            _host        = config["Email:Host"]        ?? "smtp.gmail.com";
            _port        = int.Parse(config["Email:Port"] ?? "587");
            _senderEmail = config["Email:SenderEmail"] ?? "";
            _senderName  = config["Email:SenderName"]  ?? "منصة اطمئن";
            _username    = config["Email:Username"]    ?? "";
            _password    = config["Email:Password"]    ?? "";
            _enableSsl   = bool.Parse(config["Email:EnableSsl"] ?? "true");
        }

        // ─────────────────────────────────────────────────────────────────
        // Core send helpers
        // ─────────────────────────────────────────────────────────────────

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            await SendInternalAsync(toEmail, toName, subject, htmlBody);
        }

        public async Task SendEmailWithPdfAttachmentAsync(
            string toEmail, string toName, string subject, string htmlBody,
            byte[] pdfBytes, string pdfFileName)
        {
            await SendInternalAsync(toEmail, toName, subject, htmlBody, pdfBytes, pdfFileName);
        }

        // ─────────────────────────────────────────────────────────────────
        // Transactional email methods
        // ─────────────────────────────────────────────────────────────────

        public async Task SendAccountActivationEmailAsync(
            string toEmail, string toName, string activationLink, string role)
        {
            var subject = "✅ تفعيل حسابك في منصة اطمئن";
            var body = EmailTemplates.AccountActivation(toName, activationLink, role);
            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Account activation email sent to {Email} (role: {Role})", toEmail, role);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string toName, string role)
        {
            var subject = "🎉 مرحباً بك في منصة اطمئن!";
            var body = EmailTemplates.Welcome(toName, role);
            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Welcome email sent to {Email} (role: {Role})", toEmail, role);
        }

        public async Task SendLabResultEmailAsync(
            string toEmail, string toName, string testName, DateTime testDate, byte[] reportPdf)
        {
            var subject = $"🔬 نتيجة تحليلك: {testName}";
            var body = EmailTemplates.LabResult(toName, testName, testDate);
            await SendEmailWithPdfAttachmentAsync(toEmail, toName, subject, body, reportPdf, $"تقرير-{testName}.pdf");
            _logger.LogInformation("Lab result email sent to {Email} for test {TestName}", toEmail, testName);
        }

        public async Task SendAppointmentConfirmationEmailAsync(
            string toEmail, string toName,
            string doctorName, string patientName,
            DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime,
            string? notes, bool isDoctor, byte[]? appointmentPdf = null)
        {
            var subject = "📅 تأكيد حجز الموعد — منصة اطمئن";
            var body = EmailTemplates.AppointmentConfirmation(
                toName, doctorName, patientName, appointmentDate, startTime, endTime, notes, isDoctor);

            if (appointmentPdf != null)
                await SendEmailWithPdfAttachmentAsync(toEmail, toName, subject, body, appointmentPdf, "تفاصيل-الموعد.pdf");
            else
                await SendEmailAsync(toEmail, toName, subject, body);

            _logger.LogInformation("Appointment confirmation sent to {Email} (isDoctor: {IsDoctor})", toEmail, isDoctor);
        }

        public async Task SendAppointmentReminderEmailAsync(
            string toEmail, string toName,
            string doctorName, string patientName,
            DateTime appointmentDate, TimeSpan startTime,
            string timeRemainingLabel)
        {
            var subject = $"⏰ تذكير بموعدك — بعد {timeRemainingLabel}";
            var body = EmailTemplates.AppointmentReminder(toName, doctorName, patientName, appointmentDate, startTime, timeRemainingLabel);
            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Appointment reminder ({Time}) sent to {Email}", timeRemainingLabel, toEmail);
        }

        public async Task SendAppointmentCancellationEmailAsync(
            string toEmail, string toName,
            string doctorName, string patientName,
            DateTime appointmentDate, TimeSpan startTime,
            bool isDoctor)
        {
            var subject = "❌ تم إلغاء الموعد — منصة اطمئن";
            var body = EmailTemplates.AppointmentCancellation(toName, doctorName, patientName, appointmentDate, startTime, isDoctor);
            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Cancellation email sent to {Email}", toEmail);
        }

        public async Task SendRiskAlertEmailAsync(
            string toEmail, string toName,
            string patientName, string riskLevel, decimal riskScore,
            List<string> recommendations, bool isFamilyMember, byte[]? riskReportPdf = null)
        {
            var subject = "⚠️ تنبيه: مستوى الخطر ارتفع — منصة اطمئن";
            var body = EmailTemplates.RiskAlert(toName, patientName, riskLevel, riskScore, recommendations, isFamilyMember);

            if (riskReportPdf != null)
                await SendEmailWithPdfAttachmentAsync(toEmail, toName, subject, body, riskReportPdf, "تقرير-الخطر.pdf");
            else
                await SendEmailAsync(toEmail, toName, subject, body);

            _logger.LogInformation("Risk alert email sent to {Email} (isFamilyMember: {IsFamilyMember})", toEmail, isFamilyMember);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
        {
            var subject = "🔒 إعادة تعيين كلمة المرور — منصة اطمئن";
            var body = EmailTemplates.PasswordReset(toName, resetLink);
            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }

        public async Task SendEmergencyConfirmationEmailAsync(
            string toEmail, string toName, string emergencyType, DateTime requestTime)
        {
            var subject = "🚨 تم استلام طلب الطوارئ — منصة اطمئن";
            var body = EmailTemplates.EmergencyConfirmation(toName, emergencyType, requestTime);
            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Emergency confirmation email sent to {Email}", toEmail);
        }

        public async Task SendDoctorApprovalEmailAsync(string toEmail, string toName, bool isApproved, string? reason = null)
        {
            var subject = isApproved
                ? "✅ تم قبول طلب تسجيلك كطبيب — منصة اطمئن"
                : "❌ تم رفض طلب تسجيلك كطبيب — منصة اطمئن";
            var body = EmailTemplates.DoctorApproval(toName, isApproved, reason);
            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Doctor approval email ({Approved}) sent to {Email}", isApproved, toEmail);
        }

        // ─────────────────────────────────────────────────────────────────
        // Private SMTP sender
        // ─────────────────────────────────────────────────────────────────

        private async Task SendInternalAsync(
            string toEmail, string toName, string subject, string htmlBody,
            byte[]? pdfBytes = null, string? pdfFileName = null)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_senderEmail, _senderName);
                message.To.Add(new MailAddress(toEmail, toName));
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                if (pdfBytes != null && !string.IsNullOrWhiteSpace(pdfFileName))
                {
                    var stream = new MemoryStream(pdfBytes);
                    var attachment = new Attachment(stream, pdfFileName, "application/pdf");
                    message.Attachments.Add(attachment);
                }

                using var client = new SmtpClient(_host, _port)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_username, _password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} — Subject: {Subject}", toEmail, subject);
                // We don't rethrow — email failure should NOT break the main user flow
            }
        }
    }
}
