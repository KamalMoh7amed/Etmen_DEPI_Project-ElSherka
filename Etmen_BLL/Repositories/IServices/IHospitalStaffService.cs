using Etmen_BLL.DTOs.HospitalStaff;
using Etmen_BLL.Helpers;
using Etmen_Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etmen_BLL.Repositories.IServices
{
    public interface IHospitalStaffService
    {
        Task<ServiceResult<HospitalStaffQueueDto>> GetQueueAsync(int? providerId = null);
        Task<ServiceResult<HospitalStaffEmergencyDetailDto>> GetRequestDetailAsync(int requestId, int? providerId = null);
        Task<ServiceResult> RespondToRequestAsync(HospitalStaffEmergencyRespondDto dto);
        Task<ServiceResult> RespondToRequestAsync(HospitalStaffEmergencyRespondDto dto, string respondedByUserId);
        Task<ServiceResult> UpdateBedsAsync(HospitalStaffBedsUpdateDto dto);

        // ── Staff Management ──────────────────────────────────────────────────
        Task<ServiceResult<List<StaffProfileDto>>> GetStaffMembersAsync(int providerId);
        Task<ServiceResult> InviteStaffAsync(int providerId, string email, StaffRoleType role, StaffShiftType shift, string senderUserId, string inviteUrlPrefix);
        Task<ServiceResult> ResendInvitationAsync(int profileId, string inviteUrlPrefix);
        Task<ServiceResult> CancelInvitationAsync(int profileId);
        Task<ServiceResult> UpdateStaffAsync(int profileId, StaffRoleType role, StaffShiftType shift);
        Task<ServiceResult> RemoveStaffAsync(int profileId);
        Task<ServiceResult<string>> GenerateInviteLinkAsync(int providerId, StaffRoleType role, StaffShiftType shift, string senderUserId, string inviteUrlPrefix);
        Task<ServiceResult> AcceptInvitationAsync(string token, string userId);
        Task<ServiceResult> LogActivityAsync(int staffProfileId, string action, string? details);
        Task<ServiceResult<List<StaffActivityLogDto>>> GetLogsAsync(int providerId);
        Task<ServiceResult<StaffStatsDto>> GetStatsAsync(int providerId);
    }
}
