using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class MedicalRecordService : IMedicalRecordService
    {
        private readonly IUnitOfWork _uow;

        public MedicalRecordService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByPatientAsync(string userId)
        {
            throw new NotImplementedException("GetByPatientAsync is not implemented yet.");
        }

        public Task<ServiceResult<MedicalRecordDto>> GetByIdAsync(string userId, int recordId)
        {
            throw new NotImplementedException("GetByIdAsync is not implemented yet.");
        }

        public Task<ServiceResult<MedicalRecordDto>> GetLatestAsync(string userId)
        {
            throw new NotImplementedException("GetLatestAsync is not implemented yet.");
        }

        public Task<ServiceResult<MedicalRecordDto>> CreateAsync(string userId, MedicalRecordCreateDto dto)
        {
            throw new NotImplementedException("CreateAsync is not implemented yet.");
        }

        public Task<ServiceResult> DeleteAsync(string userId, int recordId)
        {
            throw new NotImplementedException("DeleteAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException("GetByDateRangeAsync is not implemented yet.");
        }

        public Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetWithAbnormalValuesAsync(string userId)
        {
            throw new NotImplementedException("GetWithAbnormalValuesAsync is not implemented yet.");
        }

    }
}