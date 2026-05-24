using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.DTOs.Patient;
using Etmen_BLL.DTOs.Risk;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class PatientService : IPatientService
    {
        private readonly IUnitOfWork _uow;

        public PatientService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<ProfileDto>> GetProfileAsync(string userId)
        {
            throw new NotImplementedException("GetProfileAsync is not implemented yet.");
        }

        public Task<ServiceResult<ProfileDto>> UpdateProfileAsync(string userId, ProfileDto dto)
        {
            throw new NotImplementedException("UpdateProfileAsync is not implemented yet.");
        }

        public Task<ServiceResult<DashboardDto>> GetDashboardAsync(string userId)
        {
            throw new NotImplementedException("GetDashboardAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetMedicalRecordsAsync(string userId)
        {
            throw new NotImplementedException("GetMedicalRecordsAsync is not implemented yet.");
        }

        public Task<ServiceResult<MedicalRecordDto>> GetLatestMedicalRecordAsync(string userId)
        {
            throw new NotImplementedException("GetLatestMedicalRecordAsync is not implemented yet.");
        }

        public Task<ServiceResult<MedicalRecordDto>> AddMedicalRecordAsync(string userId, MedicalRecordCreateDto dto)
        {
            throw new NotImplementedException("AddMedicalRecordAsync is not implemented yet.");
        }

        public Task<ServiceResult<RiskResultDto>> AssessRiskAsync(string userId, RiskInputDto input)
        {
            throw new NotImplementedException("AssessRiskAsync is not implemented yet.");
        }

        public Task<ServiceResult<RiskResultDto>> GetLatestRiskAssessmentAsync(string userId)
        {
            throw new NotImplementedException("GetLatestRiskAssessmentAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<RiskResultDto>>> GetRiskHistoryAsync(string userId)
        {
            throw new NotImplementedException("GetRiskHistoryAsync is not implemented yet.");
        }

    }
}