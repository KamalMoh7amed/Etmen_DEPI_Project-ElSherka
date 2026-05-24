using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class RiskService : IRiskService
    {
        private readonly IUnitOfWork _uow;

        public RiskService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<RiskResultDto>> CalculateRiskAsync(RiskInputDto dto)
        {
            throw new NotImplementedException("CalculateRiskAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<RiskResultDto>>> GetPatientRiskHistoryAsync(int patientProfileId)
        {
            throw new NotImplementedException("GetPatientRiskHistoryAsync is not implemented yet.");
        }

        public Task<ServiceResult> SaveRiskAssessmentAsync(int patientProfileId, RiskResultDto riskResult)
        {
            throw new NotImplementedException("SaveRiskAssessmentAsync is not implemented yet.");
        }

    }
}