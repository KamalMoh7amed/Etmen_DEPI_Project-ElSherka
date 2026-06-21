

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IHealthcareProviderRepository : IGenericRepository<HealthcareProvider>
    {
        Task<IEnumerable<HealthcareProvider>> GetNearbyProvidersAsync(decimal latitude, decimal longitude, decimal radiusInKm);
        Task<IEnumerable<HealthcareProvider>> GetEmergencyCentersAsync(decimal latitude, decimal longitude, decimal radiusInKm);
        Task<IEnumerable<HealthcareProvider>> GetByTypeAsync(string type);
        Task<IEnumerable<HealthcareProvider>> GetWithAvailableBedsAsync();
        Task UpdateAvailableBedsAsync(int providerId, int bedsCount);
        Task<IEnumerable<HealthcareProvider>> SearchProvidersAsync(string searchTerm, decimal? latitude, decimal? longitude);
    }
}