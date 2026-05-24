using Etmen_BLL.DTOs.Crisis;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface ICrisisRiskEngineService
    {
        Task<ServiceResult<CrisisRiskResultDto>> CalculateCrisisRiskAsync(int patientProfileId, int crisisConfigurationId);
        Task<ServiceResult<decimal>> CalculateOutbreakProbabilityAsync(decimal latitude, decimal longitude, int crisisConfigurationId);
        Task<ServiceResult<List<OutbreakZoneDto>>> GetPatientsInZoneAsync(int crisisConfigurationId);
    }
}
