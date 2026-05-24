using Etmen_BLL.DTOs.Alert;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class AlertService : IAlertService
    {
        private readonly IUnitOfWork _uow;

        public AlertService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<List<AlertDto>>> GetUserAlertsAsync(string userId)
        {
            throw new NotImplementedException("GetUserAlertsAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<AlertDto>>> GetUnreadAlertsAsync(string userId)
        {
            throw new NotImplementedException("GetUnreadAlertsAsync is not implemented yet.");
        }

        public Task<ServiceResult<AlertDto>> GetAlertByIdAsync(int alertId)
        {
            throw new NotImplementedException("GetAlertByIdAsync is not implemented yet.");
        }

        public Task<ServiceResult<AlertDto>> CreateAlertAsync(int userId, string title, string message, string alertType)
        {
            throw new NotImplementedException("CreateAlertAsync is not implemented yet.");
        }

        public Task<ServiceResult> MarkAsReadAsync(int alertId)
        {
            throw new NotImplementedException("MarkAsReadAsync is not implemented yet.");
        }

        public Task<ServiceResult> MarkAllAsReadAsync(string userId)
        {
            throw new NotImplementedException("MarkAllAsReadAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeleteAlertAsync(int alertId)
        {
            throw new NotImplementedException("DeleteAlertAsync is not implemented yet.");
        }

        public Task<ServiceResult<int>> GetUnreadCountAsync(string userId)
        {
            throw new NotImplementedException("GetUnreadCountAsync is not implemented yet.");
        }

    }
}