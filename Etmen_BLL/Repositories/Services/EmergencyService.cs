using Etmen_BLL.DTOs.Emergency;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class EmergencyService : IEmergencyService
    {
        private readonly IUnitOfWork _uow;

        public EmergencyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<EmergencyRequestDto>> CreateEmergencyRequestAsync(EmergencyRequestDto dto)
        {
            throw new NotImplementedException("CreateEmergencyRequestAsync is not implemented yet.");
        }

        public Task<ServiceResult<EmergencyRequestDto>> GetEmergencyRequestAsync(int requestId)
        {
            throw new NotImplementedException("GetEmergencyRequestAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<EmergencyTrackingDto>>> GetPendingEmergenciesAsync()
        {
            throw new NotImplementedException("GetPendingEmergenciesAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateEmergencyStatusAsync(int requestId, EmergencyUpdateDto dto)
        {
            throw new NotImplementedException("UpdateEmergencyStatusAsync is not implemented yet.");
        }

        public Task<ServiceResult<HospitalQueueDto>> GetHospitalQueueAsync()
        {
            throw new NotImplementedException("GetHospitalQueueAsync is not implemented yet.");
        }

    }
}