

namespace Etmen_DAL.Repositories.Implementations
{
    public class DoctorProviderRepository : GenericRepository<DoctorProvider>, IDoctorProviderRepository
    {
        public DoctorProviderRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<DoctorProvider>> GetByDoctorIdAsync(int doctorId)
            => await _dbSet.Include(dp => dp.HealthcareProvider)
                           .Where(dp => dp.DoctorProfileId == doctorId).ToListAsync();

        public async Task<IEnumerable<DoctorProvider>> GetByProviderIdAsync(int providerId)
            => await _dbSet.Include(dp => dp.DoctorProfile).ThenInclude(d => d.ApplicationUser)
                           .Where(dp => dp.HealthcareProviderId == providerId).ToListAsync();

        public async Task<DoctorProvider?> GetAffiliationAsync(int doctorId, int providerId)
            => await _dbSet.FirstOrDefaultAsync(dp => dp.DoctorProfileId == doctorId && dp.HealthcareProviderId == providerId);
    }
}
