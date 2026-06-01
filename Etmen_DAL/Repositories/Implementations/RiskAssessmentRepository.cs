using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class RiskAssessmentRepository : GenericRepository<RiskAssessment>, IRiskAssessmentRepository
    {
        public RiskAssessmentRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<RiskAssessment>> GetByPatientIdAsync(int patientId)
            => await _dbSet.Where(r => r.PatientProfileId == patientId).OrderByDescending(r => r.AssessmentDate).ToListAsync();

        public async Task<RiskAssessment?> GetLatestByPatientIdAsync(int patientId)
            => await _dbSet.Where(r => r.PatientProfileId == patientId).OrderByDescending(r => r.AssessmentDate).FirstOrDefaultAsync();

        public async Task<IEnumerable<RiskAssessment>> GetHighRiskPatientsAsync()
            => await _dbSet.Where(r => r.RiskLevel >= RiskLevel.High)
                           .Include(r => r.PatientProfile).ThenInclude(p => p.ApplicationUser)
                           .OrderByDescending(r => r.AssessmentDate).ToListAsync();

        public async Task<IEnumerable<RiskAssessment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
            => await _dbSet.Where(r => r.AssessmentDate >= startDate && r.AssessmentDate <= endDate).OrderByDescending(r => r.AssessmentDate).ToListAsync();

        public async Task<IEnumerable<RiskAssessment>> GetByRiskLevelAsync(RiskLevel riskLevel)
            => await _dbSet.Where(r => r.RiskLevel == riskLevel).OrderByDescending(r => r.AssessmentDate).ToListAsync();

        public async Task<int> GetRiskCountByLevelAsync(RiskLevel riskLevel)
            => await _dbSet.CountAsync(r => r.RiskLevel == riskLevel);

        public async Task<decimal> GetAverageRiskScoreAsync(int patientId)
            => await _dbSet.Where(r => r.PatientProfileId == patientId).AverageAsync(r => r.RiskScore);
    }
}