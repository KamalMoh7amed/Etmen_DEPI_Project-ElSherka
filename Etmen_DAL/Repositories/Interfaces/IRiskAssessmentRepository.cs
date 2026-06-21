

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IRiskAssessmentRepository : IGenericRepository<RiskAssessment>
    {
        Task<IEnumerable<RiskAssessment>> GetByPatientIdAsync(int patientId);
        Task<RiskAssessment?> GetLatestByPatientIdAsync(int patientId);
        Task<IEnumerable<RiskAssessment>> GetHighRiskPatientsAsync();
        Task<IEnumerable<RiskAssessment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RiskAssessment>> GetByRiskLevelAsync(RiskLevel riskLevel);
        Task<int> GetRiskCountByLevelAsync(RiskLevel riskLevel);
        Task<decimal> GetAverageRiskScoreAsync(int patientId);
    }
}