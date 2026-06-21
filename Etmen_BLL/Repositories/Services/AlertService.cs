namespace Etmen_BLL.Repositories.Services
{
    public sealed class AlertService : IAlertService
    {
        private readonly IUnitOfWork _uow;

        public AlertService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<List<AlertDto>>> GetUserAlertsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<List<AlertDto>>.Failure("User ID is required.");

                var alerts = await _uow.Alerts.GetByUserIdAsync(userId);
                var alertDtos = alerts.Adapt<List<AlertDto>>();

                return ServiceResult<List<AlertDto>>.Success(alertDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<AlertDto>>.Failure($"Failed to retrieve user alerts: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<AlertDto>>> GetUnreadAlertsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<List<AlertDto>>.Failure("User ID is required.");

                var alerts = await _uow.Alerts.GetUnreadAlertsAsync(userId);
                var alertDtos = alerts.Adapt<List<AlertDto>>();

                return ServiceResult<List<AlertDto>>.Success(alertDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<AlertDto>>.Failure($"Failed to retrieve unread alerts: {ex.Message}");
            }
        }

        public async Task<ServiceResult<AlertDto>> GetAlertByIdAsync(int alertId)
        {
            try
            {
                if (alertId <= 0)
                    return ServiceResult<AlertDto>.Failure("Valid alert ID is required.");

                var alert = await _uow.Alerts.GetByIdAsync(alertId);
                if (alert == null)
                    return ServiceResult<AlertDto>.NotFound("Alert not found.");

                var alertDto = alert.Adapt<AlertDto>();
                return ServiceResult<AlertDto>.Success(alertDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<AlertDto>.Failure($"Failed to retrieve alert: {ex.Message}");
            }
        }

        public async Task<ServiceResult<AlertDto>> CreateAlertAsync(int userId, string title, string message, string alertType)
        {
            try
            {
                if (userId <= 0)
                    return ServiceResult<AlertDto>.Failure("Valid user ID is required.");

                if (string.IsNullOrWhiteSpace(title))
                    return ServiceResult<AlertDto>.Failure("Alert title is required.");

                if (string.IsNullOrWhiteSpace(message))
                    return ServiceResult<AlertDto>.Failure("Alert message is required.");

                if (string.IsNullOrWhiteSpace(alertType))
                    return ServiceResult<AlertDto>.Failure("Alert type is required.");

                // Get the user to verify they exist
                var user = await _uow.Users.GetByIdAsync(userId.ToString());
                if (user == null)
                    return ServiceResult<AlertDto>.Failure("User not found.");

                var alert = new Alert
                {
                    UserId = userId.ToString(),
                    Title = title.Trim(),
                    Message = message.Trim(),
                    AlertType = alertType.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Alerts.AddAsync(alert);
                await _uow.CompleteAsync();

                var alertDto = alert.Adapt<AlertDto>();
                return ServiceResult<AlertDto>.Created(alertDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<AlertDto>.Failure($"Failed to create alert: {ex.Message}");
            }
        }

        public async Task<ServiceResult> MarkAsReadAsync(int alertId)
        {
            try
            {
                if (alertId <= 0)
                    return ServiceResult.Failure("Valid alert ID is required.");

                var alert = await _uow.Alerts.GetByIdAsync(alertId);
                if (alert == null)
                    return ServiceResult.NotFound("Alert not found.");

                await _uow.Alerts.MarkAsReadAsync(alertId);
                await _uow.CompleteAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to mark alert as read: {ex.Message}");
            }
        }

        public async Task<ServiceResult> MarkAllAsReadAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult.Failure("User ID is required.");

                await _uow.Alerts.MarkAllAsReadAsync(userId);
                await _uow.CompleteAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to mark all alerts as read: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAlertAsync(int alertId)
        {
            try
            {
                if (alertId <= 0)
                    return ServiceResult.Failure("Valid alert ID is required.");

                var alert = await _uow.Alerts.GetByIdAsync(alertId);
                if (alert == null)
                    return ServiceResult.NotFound("Alert not found.");

                _uow.Alerts.Remove(alert);
                await _uow.CompleteAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Failed to delete alert: {ex.Message}");
            }
        }

        public async Task<ServiceResult<int>> GetUnreadCountAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<int>.Failure("User ID is required.");

                var count = await _uow.Alerts.GetUnreadCountAsync(userId);
                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"Failed to get unread alert count: {ex.Message}");
            }
        }
    }
}