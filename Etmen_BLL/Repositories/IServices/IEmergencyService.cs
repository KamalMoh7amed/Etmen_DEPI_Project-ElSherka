
namespace Etmen_BLL.Repositories.IServices
{
    public interface IEmergencyService
    {
        Task<ServiceResult<EmergencyRequestDto>> CreateEmergencyRequestAsync(EmergencyRequestDto dto);
        Task<ServiceResult<EmergencyRequestDto>> GetEmergencyRequestAsync(int requestId);
        Task<ServiceResult<List<EmergencyTrackingDto>>> GetPendingEmergenciesAsync();
        Task<ServiceResult> UpdateEmergencyStatusAsync(int requestId, EmergencyUpdateDto dto);
        Task<ServiceResult<HospitalQueueDto>> GetHospitalQueueAsync();
    }
}
