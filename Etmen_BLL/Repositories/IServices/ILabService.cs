

namespace Etmen_BLL.Repositories.IServices
{
    public interface ILabService
    {
        // Lab Results Management
        Task<ServiceResult<LabResultDto>> GetLabResultByIdAsync(int labResultId);
        Task<ServiceResult<List<LabResultDto>>> GetPatientLabResultsAsync(int patientId);
        Task<ServiceResult<List<LabResultDto>>> GetLabResultsByDateRangeAsync(int patientId, DateTime startDate, DateTime endDate);
        
        // Lab Upload
        Task<ServiceResult<LabResultDto>> UploadLabResultAsync(LabUploadDto dto);
        Task<ServiceResult> UpdateLabResultAsync(int labResultId, LabUploadDto dto);
        Task<ServiceResult> DeleteLabResultAsync(int labResultId);
        Task<ServiceResult<LabResultDto>> CreateDemoSampleAsync(int patientId, string testType);

        // Lab Analysis
        Task<ServiceResult<Dictionary<string, object>>> AnalyzeLabResultsAsync(int patientId);
        Task<ServiceResult<List<LabResultDto>>> GetAbnormalResultsAsync(int patientId);

        // Lab Reports
        Task<ServiceResult<List<LabResultDto>>> SearchLabResultsAsync(string testName, int pageNumber = 1, int pageSize = 10);
        Task<ServiceResult<Dictionary<string, object>>> GetLabStatisticsAsync();
        
        // Verification
        Task<ServiceResult> VerifyLabResultAsync(int labResultId);
        Task<ServiceResult> RejectLabResultAsync(int labResultId, string reason);
    }
}
