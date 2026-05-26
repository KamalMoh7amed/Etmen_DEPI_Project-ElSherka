using Etmen_BLL.DTOs.CriticalCare;
using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;
using Etmen_Domain.Entities;

namespace Etmen_BLL.Repositories.IServices
{
    public interface ICriticalCareEscalationService
    {
        Task<ServiceResult<CriticalCareEscalationDto>> EscalateIfNeededAsync(
            PatientProfile patient,
            RiskAssessment riskAssessment,
            RiskInputDto input);

        Task<ServiceResult<List<CriticalCareCaseDto>>> GetCriticalCasesAsync(
            bool includeResolved = false,
            int take = 100);
    }
}
