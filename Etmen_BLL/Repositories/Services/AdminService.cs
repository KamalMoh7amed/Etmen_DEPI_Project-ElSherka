using Etmen_BLL.DTOs.Admin;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUnitOfWork uow, ILogger<AdminService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public Task<ServiceResult<PaginatedResult<UserListItemDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10)
        {
            throw new NotImplementedException("GetAllUsersAsync is not implemented yet.");
        }

        public Task<ServiceResult<UserListItemDto>> GetUserByIdAsync(int userId)
        {
            throw new NotImplementedException("GetUserByIdAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
        {
            throw new NotImplementedException("UpdateUserStatusAsync is not implemented yet.");
        }

        public Task<ServiceResult> BulkUserActionAsync(BulkUserActionDto dto)
        {
            throw new NotImplementedException("BulkUserActionAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeleteUserAsync(int userId)
        {
            throw new NotImplementedException("DeleteUserAsync is not implemented yet.");
        }

        public Task<ServiceResult<PaginatedResult<ProviderListItemDto>>> GetAllProvidersAsync(int pageNumber = 1, int pageSize = 10)
        {
            throw new NotImplementedException("GetAllProvidersAsync is not implemented yet.");
        }

        public Task<ServiceResult<ProviderListItemDto>> GetProviderByIdAsync(int providerId)
        {
            throw new NotImplementedException("GetProviderByIdAsync is not implemented yet.");
        }

        public Task<ServiceResult> CreateProviderAsync(CreateProviderDto dto)
        {
            throw new NotImplementedException("CreateProviderAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateProviderAsync(int providerId, UpdateProviderDto dto)
        {
            throw new NotImplementedException("UpdateProviderAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeleteProviderAsync(int providerId)
        {
            throw new NotImplementedException("DeleteProviderAsync is not implemented yet.");
        }

        public Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync()
        {
            throw new NotImplementedException("GetDashboardAsync is not implemented yet.");
        }

        public Task<ServiceResult<PaginatedResult<AdminReportDto>>> GetReportsAsync(int pageNumber = 1, int pageSize = 10)
        {
            throw new NotImplementedException("GetReportsAsync is not implemented yet.");
        }

        public Task<ServiceResult<AdminCrisisDto>> GetCrisisManagementAsync()
        {
            throw new NotImplementedException("GetCrisisManagementAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<ActivityLogDto>>> GetActivityLogsAsync(int pageNumber = 1, int pageSize = 20)
        {
            throw new NotImplementedException("GetActivityLogsAsync is not implemented yet.");
        }

        public Task<ServiceResult<SystemConfigDto>> GetSystemConfigAsync()
        {
            throw new NotImplementedException("GetSystemConfigAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateSystemConfigAsync(SystemConfigDto dto)
        {
            throw new NotImplementedException("UpdateSystemConfigAsync is not implemented yet.");
        }

        public Task<ServiceResult> ApproveCrisisAsync(int crisisId)
        {
            throw new NotImplementedException("ApproveCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult> RejectCrisisAsync(int crisisId, string reason)
        {
            throw new NotImplementedException("RejectCrisisAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateCrisisStatusAsync(int crisisId, string status)
        {
            throw new NotImplementedException("UpdateCrisisStatusAsync is not implemented yet.");
        }

    }
}