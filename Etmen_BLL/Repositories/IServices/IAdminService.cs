using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Etmen_BLL.DTOs.Admin;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface IAdminService
    {
        // User Management
        Task<ServiceResult<PaginatedResult<UserListItemDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10);
        Task<ServiceResult<UserListItemDto>> GetUserByIdAsync(int userId);
        Task<ServiceResult> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto);
        Task<ServiceResult> BulkUserActionAsync(BulkUserActionDto dto);
        Task<ServiceResult> DeleteUserAsync(int userId);

        // Provider Management
        Task<ServiceResult<PaginatedResult<ProviderListItemDto>>> GetAllProvidersAsync(int pageNumber = 1, int pageSize = 10);
        Task<ServiceResult<ProviderListItemDto>> GetProviderByIdAsync(int providerId);
        Task<ServiceResult> CreateProviderAsync(CreateProviderDto dto);
        Task<ServiceResult> UpdateProviderAsync(int providerId, UpdateProviderDto dto);
        Task<ServiceResult> DeleteProviderAsync(int providerId);

        // Dashboard & Reports
        Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync();
        Task<ServiceResult<PaginatedResult<AdminReportDto>>> GetReportsAsync(int pageNumber = 1, int pageSize = 10);
        Task<ServiceResult<AdminCrisisDto>> GetCrisisManagementAsync();
        Task<ServiceResult<List<ActivityLogDto>>> GetActivityLogsAsync(int pageNumber = 1, int pageSize = 20);

        // System Configuration
        Task<ServiceResult<SystemConfigDto>> GetSystemConfigAsync();
        Task<ServiceResult> UpdateSystemConfigAsync(SystemConfigDto dto);

        // Crisis Management
        Task<ServiceResult> ApproveCrisisAsync(int crisisId);
        Task<ServiceResult> RejectCrisisAsync(int crisisId, string reason);
        Task<ServiceResult> UpdateCrisisStatusAsync(int crisisId, string status);
    }
}
