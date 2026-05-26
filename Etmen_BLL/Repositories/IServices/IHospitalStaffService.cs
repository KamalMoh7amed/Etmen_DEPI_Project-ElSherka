using Etmen_BLL.DTOs.HospitalStaff;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface IHospitalStaffService
    {
        Task<ServiceResult<HospitalStaffQueueDto>> GetQueueAsync(int? providerId = null);
        Task<ServiceResult<HospitalStaffEmergencyDetailDto>> GetRequestDetailAsync(int requestId, int? providerId = null);
        Task<ServiceResult> RespondToRequestAsync(HospitalStaffEmergencyRespondDto dto);
        Task<ServiceResult> UpdateBedsAsync(HospitalStaffBedsUpdateDto dto);
    }
}
