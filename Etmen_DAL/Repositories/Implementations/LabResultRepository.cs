using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class LabResultRepository : GenericRepository<LabResult>, ILabResultRepository
    {
        public LabResultRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<LabResult>> GetByPatientIdAsync(int patientId)
            => await _dbSet.Where(l => l.PatientProfileId == patientId).OrderByDescending(l => l.TestDate).ToListAsync();

        public async Task<LabResult?> GetLatestByPatientIdAsync(int patientId)
            => await _dbSet.Where(l => l.PatientProfileId == patientId).OrderByDescending(l => l.TestDate).FirstOrDefaultAsync();

        public async Task<IEnumerable<LabResult>> GetByTestNameAsync(int patientId, string name)
            => await _dbSet.Where(l => l.PatientProfileId == patientId && l.TestName.Contains(name)).OrderByDescending(l => l.TestDate).ToListAsync();

        public async Task<IEnumerable<LabResult>> GetWithOcrDataAsync(int patientId)
            => await _dbSet.Where(l => l.PatientProfileId == patientId && !string.IsNullOrEmpty(l.OcrExtractedData)).OrderByDescending(l => l.TestDate).ToListAsync();

        public async Task<IEnumerable<LabResult>> GetByDateRangeAsync(int patientId, DateTime start, DateTime end)
            => await _dbSet.Where(l => l.PatientProfileId == patientId && l.TestDate >= start && l.TestDate <= end).OrderByDescending(l => l.TestDate).ToListAsync();

        public async Task UpdateOcrDataAsync(int labId, string data)
        {
            var lab = await _dbSet.FindAsync(labId);
            if (lab != null) { lab.OcrExtractedData = data; _dbSet.Update(lab); }
        }

        public async Task<IEnumerable<LabResult>> SearchLabResultsAsync(int patientId, string term)
            => await _dbSet.Where(l => l.PatientProfileId == patientId && (l.TestName.Contains(term) || (l.Results != null && l.Results.Contains(term)))).OrderByDescending(l => l.TestDate).ToListAsync();
    }
}