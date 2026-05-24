using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface IRiskService
    {
        Task<ServiceResult<RiskResultDto>> CalculateRiskAsync(RiskInputDto dto);
        Task<ServiceResult<List<RiskResultDto>>> GetPatientRiskHistoryAsync(int patientProfileId);
        Task<ServiceResult> SaveRiskAssessmentAsync(int patientProfileId, RiskResultDto riskResult);
    }
}
