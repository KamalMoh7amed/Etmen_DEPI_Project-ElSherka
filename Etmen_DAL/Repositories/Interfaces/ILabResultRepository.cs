using Etmen_Domain.Entities;

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface ILabResultRepository : IGenericRepository<LabResult>
    {
        Task<IEnumerable<LabResult>> GetByPatientIdAsync(int patientId);
        Task<LabResult?> GetLatestByPatientIdAsync(int patientId);
        Task<IEnumerable<LabResult>> GetByTestNameAsync(int patientId, string testName);
        Task<IEnumerable<LabResult>> GetWithOcrDataAsync(int patientId);
        Task<IEnumerable<LabResult>> GetByDateRangeAsync(int patientId, DateTime startDate, DateTime endDate);
        Task UpdateOcrDataAsync(int labResultId, string ocrData);
        Task<IEnumerable<LabResult>> SearchLabResultsAsync(int patientId, string searchTerm);
    }
}