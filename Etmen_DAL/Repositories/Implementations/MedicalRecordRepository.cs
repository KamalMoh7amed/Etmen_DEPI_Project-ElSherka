using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class MedicalRecordRepository : GenericRepository<MedicalRecord>, IMedicalRecordRepository
    {
        public MedicalRecordRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId)
            => await _dbSet.Where(m => m.PatientProfileId == patientId).OrderByDescending(m => m.RecordDate).ToListAsync();

        public async Task<MedicalRecord?> GetLatestByPatientIdAsync(int patientId)
            => await _dbSet.Where(m => m.PatientProfileId == patientId).OrderByDescending(m => m.RecordDate).FirstOrDefaultAsync();

        public async Task<IEnumerable<MedicalRecord>> GetByDateRangeAsync(int patientId, DateTime startDate, DateTime endDate)
            => await _dbSet.Where(m => m.PatientProfileId == patientId && m.RecordDate >= startDate && m.RecordDate <= endDate)
                           .OrderByDescending(m => m.RecordDate).ToListAsync();

        public async Task<IEnumerable<MedicalRecord>> GetWithAbnormalValuesAsync(int patientId)
            => await _dbSet.Where(m => m.PatientProfileId == patientId &&
                (m.SystolicBP >= 140 || m.DiastolicBP >= 90 || m.BloodSugar >= 200 || m.HeartRate >= 100 || m.Temperature >= 38))
                           .OrderByDescending(m => m.RecordDate).ToListAsync();

        public async Task AddRecordWithSymptomsAsync(MedicalRecord record, IEnumerable<string> symptoms)
        {
            record.Symptoms = string.Join(", ", symptoms);
            await AddAsync(record);
        }
    }
}