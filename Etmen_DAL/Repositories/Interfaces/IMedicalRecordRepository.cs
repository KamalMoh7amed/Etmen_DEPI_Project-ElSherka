

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IMedicalRecordRepository : IGenericRepository<MedicalRecord>
    {
        Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId);
        Task<MedicalRecord?> GetLatestByPatientIdAsync(int patientId);
        Task<IEnumerable<MedicalRecord>> GetByDateRangeAsync(int patientId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<MedicalRecord>> GetWithAbnormalValuesAsync(int patientId);
        Task AddRecordWithSymptomsAsync(MedicalRecord record, IEnumerable<string> symptoms);
    }
}