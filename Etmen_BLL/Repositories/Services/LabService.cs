using Etmen_BLL.DTOs.Lab;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class LabService : ILabService
    {
        private readonly IUnitOfWork _uow;

        public LabService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<LabResultDto>> GetLabResultByIdAsync(int labResultId)
        {
            throw new NotImplementedException("GetLabResultByIdAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<LabResultDto>>> GetPatientLabResultsAsync(int patientId)
        {
            throw new NotImplementedException("GetPatientLabResultsAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<LabResultDto>>> GetLabResultsByDateRangeAsync(int patientId, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException("GetLabResultsByDateRangeAsync is not implemented yet.");
        }

        public Task<ServiceResult<LabResultDto>> UploadLabResultAsync(LabUploadDto dto)
        {
            throw new NotImplementedException("UploadLabResultAsync is not implemented yet.");
        }

        public Task<ServiceResult> UpdateLabResultAsync(int labResultId, LabUploadDto dto)
        {
            throw new NotImplementedException("UpdateLabResultAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeleteLabResultAsync(int labResultId)
        {
            throw new NotImplementedException("DeleteLabResultAsync is not implemented yet.");
        }

        public Task<ServiceResult<Dictionary<string, object>>> AnalyzeLabResultsAsync(int patientId)
        {
            throw new NotImplementedException("AnalyzeLabResultsAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<LabResultDto>>> GetAbnormalResultsAsync(int patientId)
        {
            throw new NotImplementedException("GetAbnormalResultsAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<LabResultDto>>> SearchLabResultsAsync(string testName, int pageNumber = 1, int pageSize = 10)
        {
            throw new NotImplementedException("SearchLabResultsAsync is not implemented yet.");
        }

        public Task<ServiceResult<Dictionary<string, object>>> GetLabStatisticsAsync()
        {
            throw new NotImplementedException("GetLabStatisticsAsync is not implemented yet.");
        }

        public Task<ServiceResult> VerifyLabResultAsync(int labResultId)
        {
            throw new NotImplementedException("VerifyLabResultAsync is not implemented yet.");
        }

        public Task<ServiceResult> RejectLabResultAsync(int labResultId, string reason)
        {
            throw new NotImplementedException("RejectLabResultAsync is not implemented yet.");
        }

    }
}