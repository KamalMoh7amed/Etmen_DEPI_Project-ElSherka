using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Etmen_BLL.Repositories.Services
{
    /// <summary>
    /// Background service that runs every 30 minutes to check upcoming appointments
    /// and sends reminder emails:
    ///   • 24 hours before  → "موعدك بعد يوم"
    ///   •  2 hours before  → "موعدك بعد ساعتين"
    /// Uses ReminderSentOneDayBefore / ReminderSentTwoHoursBefore flags so each
    /// reminder is sent exactly once.
    /// </summary>
    public sealed class AppointmentReminderHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderHostedService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

        public AppointmentReminderHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentReminderHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AppointmentReminderHostedService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AppointmentReminderHostedService cycle.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("AppointmentReminderHostedService stopping.");
        }

        private async Task ProcessRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var uow         = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var emailSvc    = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;

            // Fetch all scheduled (non-cancelled) appointments in the next 26 hours
            var all = await uow.Appointments.GetAllAsync();
            var upcoming = all.Where(a =>
                a.Status == AppointmentStatus.Scheduled &&
                a.AppointmentDate >= now.Date).ToList();

            int sentCount = 0;

            foreach (var appt in upcoming)
            {
                // Build the exact DateTime of the appointment
                var apptDateTime = appt.AppointmentDate.Date + appt.StartTime;

                var hoursUntil = (apptDateTime - now).TotalHours;

                // ── 24-hour reminder ──────────────────────────────────────
                if (!appt.ReminderSentOneDayBefore && hoursUntil is > 0 and <= 26)
                {
                    await SendReminderAsync(emailSvc, uow, appt, "يوم كامل");
                    appt.ReminderSentOneDayBefore = true;
                    uow.Appointments.Update(appt);
                    sentCount++;
                    _logger.LogInformation(
                        "24h reminder sent for appointment {Id} at {Time}", appt.Id, apptDateTime);
                }

                // ── 2-hour reminder ───────────────────────────────────────
                if (!appt.ReminderSentTwoHoursBefore && hoursUntil is > 0 and <= 2.5)
                {
                    await SendReminderAsync(emailSvc, uow, appt, "ساعتين");
                    appt.ReminderSentTwoHoursBefore = true;
                    uow.Appointments.Update(appt);
                    sentCount++;
                    _logger.LogInformation(
                        "2h reminder sent for appointment {Id} at {Time}", appt.Id, apptDateTime);
                }
            }

            if (sentCount > 0)
                await uow.CompleteAsync();

            _logger.LogDebug("Reminder cycle done. Sent {Count} reminders.", sentCount);
        }

        private static async Task SendReminderAsync(
            IEmailService emailSvc, IUnitOfWork uow,
            Etmen_Domain.Entities.Appointment appt, string timeLabel)
        {
            var patient    = appt.PatientProfile ?? await uow.PatientProfiles.Table.Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.Id == appt.PatientProfileId);
            var doctor     = appt.DoctorProfile  ?? (appt.DoctorProfileId.HasValue
                                ? await uow.DoctorProfiles.Table.Include(d => d.ApplicationUser).FirstOrDefaultAsync(d => d.Id == appt.DoctorProfileId.Value)
                                : null);

            var patientEmail = patient?.ApplicationUser?.Email;
            var patientName  = patient?.FullName ?? "المريض";
            var doctorName   = doctor?.FullName  ?? "الطبيب";

            if (!string.IsNullOrWhiteSpace(patientEmail))
            {
                await emailSvc.SendAppointmentReminderEmailAsync(
                    patientEmail, patientName,
                    doctorName, patientName,
                    appt.AppointmentDate, appt.StartTime,
                    timeLabel);
            }

            // Also remind the doctor
            var doctorEmail = doctor?.ApplicationUser?.Email;
            if (!string.IsNullOrWhiteSpace(doctorEmail))
            {
                await emailSvc.SendAppointmentReminderEmailAsync(
                    doctorEmail, $"د. {doctorName}",
                    doctorName, patientName,
                    appt.AppointmentDate, appt.StartTime,
                    timeLabel);
            }
        }
    }
}
