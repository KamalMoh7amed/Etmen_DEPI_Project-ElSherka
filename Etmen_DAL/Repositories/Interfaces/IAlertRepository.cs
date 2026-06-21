

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IAlertRepository : IGenericRepository<Alert>
    {
        Task<IEnumerable<Alert>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Alert>> GetUnreadAlertsAsync(string userId);
        Task<IEnumerable<Alert>> GetByTypeAsync(string userId, string alertType);
        Task MarkAsReadAsync(int alertId);
        Task MarkAllAsReadAsync(string userId);
        Task DismissAlertAsync(int alertId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<Alert>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
    }
}