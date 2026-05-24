using Etmen_BLL.DTOs.Crisis;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class CrisisRiskEngineService : ICrisisRiskEngineService
    {
        private readonly IUnitOfWork _uow;

        public CrisisRiskEngineService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<CrisisRiskResultDto>> CalculateCrisisRiskAsync(int patientProfileId, int crisisConfigurationId)
        {
            throw new NotImplementedException("CalculateCrisisRiskAsync is not implemented yet.");
        }

        public Task<ServiceResult<decimal>> CalculateOutbreakProbabilityAsync(decimal latitude, decimal longitude, int crisisConfigurationId)
        {
            throw new NotImplementedException("CalculateOutbreakProbabilityAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<OutbreakZoneDto>>> GetPatientsInZoneAsync(int crisisConfigurationId)
        {
            throw new NotImplementedException("GetPatientsInZoneAsync is not implemented yet.");
        }

    }
}