

namespace Etmen_DAL.Repositories.Implementations
{
    public class EmergencyRequestRepository : GenericRepository<EmergencyRequest>, IEmergencyRequestRepository
    {
        public EmergencyRequestRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<EmergencyRequest>> GetByPatientIdAsync(int patientId)
            => await _dbSet.Include(e => e.HealthcareProvider).Where(e => e.PatientProfileId == patientId).OrderByDescending(e => e.RequestedAt).ToListAsync();

        public async Task<IEnumerable<EmergencyRequest>> GetByProviderIdAsync(int providerId)
            => await _dbSet.Include(e => e.PatientProfile).ThenInclude(p => p.ApplicationUser).Where(e => e.HealthcareProviderId == providerId).OrderByDescending(e => e.RequestedAt).ToListAsync();

        public async Task<IEnumerable<EmergencyRequest>> GetPendingRequestsAsync()
            => await _dbSet.Include(e => e.PatientProfile).ThenInclude(p => p.ApplicationUser).Where(e => e.Status == EmergencyRequestStatus.Pending).OrderBy(e => e.RequestedAt).ToListAsync();

        public async Task<IEnumerable<EmergencyRequest>> GetByStatusAsync(EmergencyRequestStatus status)
            => await _dbSet.Where(e => e.Status == status).OrderByDescending(e => e.RequestedAt).ToListAsync();

        public async Task<EmergencyRequest?> GetWithTrackingInfoAsync(int id)
            => await _dbSet.Include(e => e.PatientProfile).ThenInclude(p => p.ApplicationUser).Include(e => e.HealthcareProvider).FirstOrDefaultAsync(e => e.Id == id);

        public async Task AcceptRequestAsync(int id, int providerId)
        {
            var req = await _dbSet.FindAsync(id);
            if (req != null) { req.Status = EmergencyRequestStatus.Accepted; req.HealthcareProviderId = providerId; req.AcceptedAt = DateTime.UtcNow; _dbSet.Update(req); }
        }

        public async Task RejectRequestAsync(int id, string reason)
        {
            var req = await _dbSet.FindAsync(id);
            if (req != null) { req.Status = EmergencyRequestStatus.Rejected; req.ResponseNotes = reason; _dbSet.Update(req); }
        }

        public async Task CompleteRequestAsync(int id, string notes)
        {
            var req = await _dbSet.FindAsync(id);
            if (req != null) { req.Status = EmergencyRequestStatus.Completed; req.CompletedAt = DateTime.UtcNow; req.ResponseNotes = notes; _dbSet.Update(req); }
        }

        public async Task<IEnumerable<EmergencyRequest>> GetByDateRangeAsync(DateTime start, DateTime end)
            => await _dbSet.Where(e => e.RequestedAt >= start && e.RequestedAt <= end).OrderByDescending(e => e.RequestedAt).ToListAsync();

        public async Task<int> GetPendingCountAsync()
            => await _dbSet.CountAsync(e => e.Status == EmergencyRequestStatus.Pending);
    }
}