
namespace Etmen_DAL.Repositories.Interfaces
{
    public interface ICrisisConfigurationRepository : IGenericRepository<CrisisConfiguration>
    {
        Task<CrisisConfiguration?> GetActiveCrisisAsync();
        Task<CrisisConfiguration?> GetWithOutbreakZonesAsync(int crisisId);
        Task<CrisisConfiguration?> GetWithSymptomWeightsAsync(int crisisId);
        Task<IEnumerable<CrisisConfiguration>> GetAllCrisesAsync();
        Task<IEnumerable<CrisisConfiguration>> GetByTypeAsync(CrisisType crisisType);
        Task<IEnumerable<CrisisConfiguration>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task ActivateCrisisAsync(int crisisId);
        Task DeactivateCrisisAsync(int crisisId);
        Task UpdateSystemModeAsync(int crisisId, SystemMode mode);
    }
}