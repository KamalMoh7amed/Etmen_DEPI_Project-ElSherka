using Etmen_BLL.DTOs.Notification;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;

        public NotificationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<NotificationDto>> GetNotificationByIdAsync(int notificationId)
        {
            var notification = await _uow.Notifications.GetByIdAsync(notificationId);
            return notification is null
                ? ServiceResult<NotificationDto>.NotFound("Notification was not found.")
                : ServiceResult<NotificationDto>.Success(Map(notification));
        }

        public async Task<ServiceResult<List<NotificationDto>>> GetUserNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            var normalizedUserId = NormalizeUserId(userId);
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var notifications = await _uow.Notifications.Table
                .Where(n => n.UserId == normalizedUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<List<NotificationDto>>.Success(notifications.Select(Map).ToList());
        }

        public async Task<ServiceResult<NotificationDto>> CreateNotificationAsync(int userId, string title, string message, string type)
        {
            var result = await CreateNotificationCoreAsync(NormalizeUserId(userId), title, message, BuildLink(type));
            return result.IsSuccess
                ? ServiceResult<NotificationDto>.Created(result.Data!)
                : ServiceResult<NotificationDto>.Failure(result.ErrorMessage ?? "Could not create notification.", result.StatusCode);
        }

        public async Task<ServiceResult> MarkAsReadAsync(int notificationId)
        {
            if (!await _uow.Notifications.AnyAsync(n => n.Id == notificationId))
                return ServiceResult.NotFound("Notification was not found.");

            await _uow.Notifications.MarkAsReadAsync(notificationId);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> MarkAllAsReadAsync(int userId)
        {
            await _uow.Notifications.MarkAllAsReadAsync(NormalizeUserId(userId));
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteNotificationAsync(int notificationId)
        {
            var notification = await _uow.Notifications.GetByIdAsync(notificationId);
            if (notification is null)
                return ServiceResult.NotFound("Notification was not found.");

            _uow.Notifications.Remove(notification);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SendAppointmentReminderAsync(int appointmentId)
        {
            var appointment = await _uow.Appointments.GetWithDetailsAsync(appointmentId);
            if (appointment is null)
                return ServiceResult.NotFound("Appointment was not found.");

            var patientUserId = appointment.PatientProfile.ApplicationUserId;
            var doctorUserId = appointment.DoctorProfile?.ApplicationUserId;
            var when = $"{appointment.AppointmentDate:yyyy-MM-dd} {appointment.StartTime:hh\\:mm}";

            await CreateNotificationEntityAsync(patientUserId, "Appointment reminder", $"You have an appointment scheduled on {when}.", $"/Patient/Appointments/{appointment.Id}");
            if (!string.IsNullOrWhiteSpace(doctorUserId))
                await CreateNotificationEntityAsync(doctorUserId, "Upcoming appointment", $"You have a patient appointment scheduled on {when}.", $"/Doctor/Appointments/{appointment.Id}");

            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SendAlertNotificationAsync(int alertId)
        {
            var alert = await _uow.Alerts.GetByIdAsync(alertId);
            if (alert is null)
                return ServiceResult.NotFound("Alert was not found.");

            await CreateNotificationEntityAsync(alert.UserId, alert.Title, alert.Message, $"/Patient/Alerts/{alert.Id}");
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SendEmergencyNotificationAsync(int emergencyRequestId)
        {
            var request = await _uow.EmergencyRequests.GetWithTrackingInfoAsync(emergencyRequestId);
            if (request is null)
                return ServiceResult.NotFound("Emergency request was not found.");

            await CreateNotificationEntityAsync(
                request.PatientProfile.ApplicationUserId,
                "Emergency request update",
                $"Emergency request #{request.Id} is currently {request.Status}.",
                $"/Patient/Emergency/{request.Id}");

            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SendCrisisAlertAsync(int crisisId, List<int> userIds)
        {
            var crisis = await _uow.CrisisConfigurations.GetByIdAsync(crisisId);
            if (crisis is null)
                return ServiceResult.NotFound("Crisis was not found.");

            if (userIds is null || userIds.Count == 0)
                return ServiceResult.Failure("At least one user id is required.");

            foreach (var userId in userIds.Distinct())
            {
                await CreateNotificationEntityAsync(
                    NormalizeUserId(userId),
                    $"Crisis alert: {crisis.CrisisName}",
                    crisis.Description ?? "A public-health crisis alert has been issued. Please review your risk guidance.",
                    $"/Crisis/{crisis.Id}");
            }

            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SendFamilyInvitationAsync(int familyLinkId)
        {
            var familyLink = await _uow.FamilyLinks.Table
                .Include(f => f.LinkedPatient)
                .FirstOrDefaultAsync(f => f.Id == familyLinkId);
            if (familyLink is null)
                return ServiceResult.NotFound("Family invitation was not found.");

            await CreateNotificationEntityAsync(
                familyLink.LinkedPatient.ApplicationUserId,
                "Family invitation",
                "You have been invited to connect family health profiles.",
                $"/Family/Accept?token={Uri.EscapeDataString(familyLink.InviteToken)}");

            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SendBulkNotificationAsync(List<int> userIds, string title, string message)
        {
            if (userIds is null || userIds.Count == 0)
                return ServiceResult.Failure("At least one user id is required.");

            var errors = ValidateMessage(title, message).ToList();
            if (errors.Count > 0)
                return ServiceResult.Failure(errors);

            foreach (var userId in userIds.Distinct())
                await CreateNotificationEntityAsync(NormalizeUserId(userId), title, message, null);

            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ClearExpiredNotificationsAsync()
        {
            var cutoff = DateTime.UtcNow.AddDays(-90);
            var expired = await _uow.Notifications.Table
                .Where(n => n.IsRead && n.CreatedAt < cutoff)
                .ToListAsync();

            if (expired.Count == 0)
                return ServiceResult.Success();

            _uow.Notifications.RemoveRange(expired);
            await _uow.CompleteAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<int>> GetUnreadCountAsync(int userId)
        {
            var count = await _uow.Notifications.GetUnreadCountAsync(NormalizeUserId(userId));
            return ServiceResult<int>.Success(count);
        }

        public async Task<ServiceResult<Dictionary<string, int>>> GetNotificationStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var stats = new Dictionary<string, int>
            {
                ["total"] = await _uow.Notifications.CountAsync(),
                ["unread"] = await _uow.Notifications.CountAsync(n => !n.IsRead),
                ["read"] = await _uow.Notifications.CountAsync(n => n.IsRead),
                ["createdToday"] = await _uow.Notifications.CountAsync(n => n.CreatedAt.Date == now.Date),
                ["createdThisMonth"] = await _uow.Notifications.CountAsync(n => n.CreatedAt.Year == now.Year && n.CreatedAt.Month == now.Month)
            };

            return ServiceResult<Dictionary<string, int>>.Success(stats);
        }

        private async Task<ServiceResult<NotificationDto>> CreateNotificationCoreAsync(string userId, string title, string message, string? link)
        {
            var errors = ValidateMessage(title, message).ToList();
            if (string.IsNullOrWhiteSpace(userId))
                errors.Add("User id is required.");
            if (errors.Count > 0)
                return ServiceResult<NotificationDto>.Failure(errors);

            var notification = await CreateNotificationEntityAsync(userId, title, message, link);
            await _uow.CompleteAsync();
            return ServiceResult<NotificationDto>.Created(Map(notification));
        }

        private async Task<Notification> CreateNotificationEntityAsync(string userId, string title, string message, string? link)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title.Trim(),
                Message = message.Trim(),
                Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Notifications.AddAsync(notification);
            return notification;
        }

        private static NotificationDto Map(Notification notification) => new()
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            IsRead = notification.IsRead,
            Link = notification.Link,
            CreatedAt = notification.CreatedAt
        };

        private static IEnumerable<string> ValidateMessage(string title, string message)
        {
            if (string.IsNullOrWhiteSpace(title))
                yield return "Notification title is required.";
            if (string.IsNullOrWhiteSpace(message))
                yield return "Notification message is required.";
            if (title?.Length > 300)
                yield return "Notification title cannot exceed 300 characters.";
            if (message?.Length > 1000)
                yield return "Notification message cannot exceed 1000 characters.";
        }

        private static string NormalizeUserId(int userId) => userId.ToString();

        private static string? BuildLink(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return null;

            return type.Trim().ToLowerInvariant() switch
            {
                "appointment" => "/Patient/Appointments",
                "alert" => "/Patient/Alerts",
                "emergency" => "/Patient/Emergency",
                "crisis" => "/Crisis",
                "family" => "/Family",
                _ => null
            };
        }
    }
}
