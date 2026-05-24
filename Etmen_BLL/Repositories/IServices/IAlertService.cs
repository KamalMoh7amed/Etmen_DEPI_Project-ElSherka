using Etmen_BLL.DTOs.Alert;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface IAlertService
    {
        Task<ServiceResult<List<AlertDto>>> GetUserAlertsAsync(string userId);
        Task<ServiceResult<List<AlertDto>>> GetUnreadAlertsAsync(string userId);
        Task<ServiceResult<AlertDto>> GetAlertByIdAsync(int alertId);
        Task<ServiceResult<AlertDto>> CreateAlertAsync(int userId, string title, string message, string alertType);
        Task<ServiceResult> MarkAsReadAsync(int alertId);
        Task<ServiceResult> MarkAllAsReadAsync(string userId);
        Task<ServiceResult> DeleteAlertAsync(int alertId);
        Task<ServiceResult<int>> GetUnreadCountAsync(string userId);
    }
}
