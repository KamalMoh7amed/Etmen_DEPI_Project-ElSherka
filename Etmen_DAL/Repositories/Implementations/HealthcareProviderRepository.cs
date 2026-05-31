using Etmen_DAL.Repositories.Interfaces;
using Etmen_DAL.Helpers;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Etmen_DAL.DbContext;

namespace Etmen_DAL.Repositories.Implementations
{
    public class HealthcareProviderRepository : GenericRepository<HealthcareProvider>, IHealthcareProviderRepository
    {
        public HealthcareProviderRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<HealthcareProvider>> GetNearbyProvidersAsync(decimal latitude, decimal longitude, decimal radiusInKm)
        {
            var providers = await _dbSet.Where(p => p.IsActive).ToListAsync();
            return providers.Where(p => GeoHelper.CalculateDistance(latitude, longitude, p.Latitude, p.Longitude) <= radiusInKm)
                            .OrderBy(p => GeoHelper.CalculateDistance(latitude, longitude, p.Latitude, p.Longitude));
        }

        public async Task<IEnumerable<HealthcareProvider>> GetEmergencyCentersAsync(decimal latitude, decimal longitude, decimal radiusInKm)
        {
            var centers = await _dbSet.Where(p => p.IsEmergencyCenter && p.IsActive && p.AvailableBeds > 0).ToListAsync();
            return centers.Where(p => GeoHelper.CalculateDistance(latitude, longitude, p.Latitude, p.Longitude) <= radiusInKm)
                          .OrderBy(p => GeoHelper.CalculateDistance(latitude, longitude, p.Latitude, p.Longitude));
        }

        public async Task<IEnumerable<HealthcareProvider>> GetByTypeAsync(string type)
            => await _dbSet.Where(p => p.Type == type && p.IsActive).ToListAsync();

        public async Task<IEnumerable<HealthcareProvider>> GetWithAvailableBedsAsync()
            => await _dbSet.Where(p => p.AvailableBeds > 0 && p.IsActive).OrderByDescending(p => p.AvailableBeds).ToListAsync();

        public async Task UpdateAvailableBedsAsync(int providerId, int bedsCount)
        {
            var provider = await _dbSet.FindAsync(providerId);
            if (provider != null)
            {
                provider.AvailableBeds = bedsCount;
                _dbSet.Update(provider);
            }
        }

        public async Task<IEnumerable<HealthcareProvider>> SearchProvidersAsync(string searchTerm, decimal? latitude, decimal? longitude)
        {
            var query = _dbSet.Where(p => p.IsActive && (p.Name.Contains(searchTerm) || p.Address.Contains(searchTerm))).AsEnumerable();
            return (latitude.HasValue && longitude.HasValue)
                ? query.OrderBy(p => GeoHelper.CalculateDistance(latitude.Value, longitude.Value, p.Latitude, p.Longitude))
                : query;
        }
    }
}