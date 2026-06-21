

namespace Etmen_BLL.Repositories.IServices
{
    public interface INotificationService
    {
        // Notification CRUD
        Task<ServiceResult<NotificationDto>> GetNotificationByIdAsync(int notificationId);
        Task<ServiceResult<List<NotificationDto>>> GetUserNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 10);
        Task<ServiceResult<NotificationDto>> CreateNotificationAsync(int userId, string title, string message, string type);
        Task<ServiceResult> MarkAsReadAsync(int notificationId);
        Task<ServiceResult> MarkAllAsReadAsync(int userId);
        Task<ServiceResult> DeleteNotificationAsync(int notificationId);

        // Notification Sending
        Task<ServiceResult> SendAppointmentReminderAsync(int appointmentId);
        Task<ServiceResult> SendAlertNotificationAsync(int alertId);
        Task<ServiceResult> SendEmergencyNotificationAsync(int emergencyRequestId);
        Task<ServiceResult> SendCrisisAlertAsync(int crisisId, List<int> userIds);
        Task<ServiceResult> SendFamilyInvitationAsync(int familyLinkId);

        // Bulk Operations
        Task<ServiceResult> SendBulkNotificationAsync(List<int> userIds, string title, string message);
        Task<ServiceResult> ClearExpiredNotificationsAsync();

        // Statistics
        Task<ServiceResult<int>> GetUnreadCountAsync(int userId);
        Task<ServiceResult<Dictionary<string, int>>> GetNotificationStatisticsAsync();
    }
}
