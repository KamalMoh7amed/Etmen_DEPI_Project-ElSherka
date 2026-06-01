using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class PatientProfileRepository : GenericRepository<PatientProfile>, IPatientProfileRepository
    {
        public PatientProfileRepository(EtmenDbContext context) : base(context) { }

        public async Task<PatientProfile?> GetByUserIdAsync(string userId)
            => await _dbSet.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        public async Task<PatientProfile?> GetWithMedicalRecordsAsync(string userId)
            => await _dbSet.Include(p => p.MedicalRecords.OrderByDescending(m => m.RecordDate)).FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        public async Task<PatientProfile?> GetWithRiskAssessmentsAsync(string userId)
            => await _dbSet.Include(p => p.RiskAssessments.OrderByDescending(r => r.AssessmentDate).Take(10)).FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        public async Task<PatientProfile?> GetWithAppointmentsAsync(string userId)
            => await _dbSet.Include(p => p.Appointments.Where(a => a.Status ==AppointmentStatus.Scheduled))
                           .ThenInclude(a => a.DoctorProfile).FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        public async Task<PatientProfile?> GetWithFamilyLinksAsync(int patientId)
            => await _dbSet.Include(p => p.PrimaryLinks).ThenInclude(l => l.LinkedPatient)
                           .Include(p => p.LinkedLinks).ThenInclude(l => l.PrimaryPatient)
                           .FirstOrDefaultAsync(p => p.Id == patientId);

        public async Task<IEnumerable<PatientProfile>> GetFamilyMembersAsync(int patientId)
        {
            var links = await _context.FamilyLinks
                .Where(f => f.PrimaryPatientId == patientId || f.LinkedPatientId == patientId).ToListAsync();

            var memberIds = links.Select(f => f.PrimaryPatientId == patientId ? f.LinkedPatientId : f.PrimaryPatientId).ToList();
            return await _dbSet.Where(p => memberIds.Contains(p.Id)).ToListAsync();
        }

        public async Task<decimal?> GetLatestBmiAsync(string userId)
        {
            var patient = await GetByUserIdAsync(userId);
            return patient?.BMI;
        }
    }
}