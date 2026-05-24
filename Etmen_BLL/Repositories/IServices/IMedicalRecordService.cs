using Etmen_BLL.DTOs.Medical;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    /// <summary>
    /// Contract for medical record CRUD and historical trend queries.
    /// </summary>
    public interface IMedicalRecordService
    {
        Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByPatientAsync(string userId);

        Task<ServiceResult<MedicalRecordDto>> GetByIdAsync(string userId, int recordId);

        Task<ServiceResult<MedicalRecordDto>> GetLatestAsync(string userId);

        Task<ServiceResult<MedicalRecordDto>> CreateAsync(string userId, MedicalRecordCreateDto dto);

        Task<ServiceResult> DeleteAsync(string userId, int recordId);

        Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetByDateRangeAsync(
            string userId, DateTime startDate, DateTime endDate);

        Task<ServiceResult<IEnumerable<MedicalRecordDto>>> GetWithAbnormalValuesAsync(string userId);
    }
}
