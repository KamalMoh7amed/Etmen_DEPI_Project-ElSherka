using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Etmen_Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class CrisisConfigurationRepository : GenericRepository<CrisisConfiguration>, ICrisisConfigurationRepository
    {
        public CrisisConfigurationRepository(EtmenDbContext context) : base(context) { }

        public async Task<CrisisConfiguration?> GetActiveCrisisAsync()
            => await _dbSet.Include(c => c.OutbreakZones).Include(c => c.SymptomWeights).FirstOrDefaultAsync(c => c.IsActive && c.SystemMode != SystemMode.Normal);

        public async Task<CrisisConfiguration?> GetWithOutbreakZonesAsync(int id)
            => await _dbSet.Include(c => c.OutbreakZones).FirstOrDefaultAsync(c => c.Id == id);

        public async Task<CrisisConfiguration?> GetWithSymptomWeightsAsync(int id)
            => await _dbSet.Include(c => c.SymptomWeights).FirstOrDefaultAsync(c => c.Id == id);

        public async Task<IEnumerable<CrisisConfiguration>> GetAllCrisesAsync()
            => await _dbSet.OrderByDescending(c => c.CreatedAt).ToListAsync();

        public async Task<IEnumerable<CrisisConfiguration>> GetByTypeAsync(CrisisType type)
            => await _dbSet.Where(c => c.CrisisType == type).OrderByDescending(c => c.CreatedAt).ToListAsync();

        public async Task<IEnumerable<CrisisConfiguration>> GetByDateRangeAsync(DateTime start, DateTime end)
            => await _dbSet.Where(c => c.StartDate >= start && c.StartDate <= end).OrderByDescending(c => c.CreatedAt).ToListAsync();

        public async Task ActivateCrisisAsync(int id)
        {
            var crisis = await _dbSet.FindAsync(id);
            if (crisis != null)
            {
                var all = await _dbSet.ToListAsync();
                foreach (var c in all) if (c.Id != id && c.IsActive) { c.IsActive = false; c.UpdatedAt = DateTime.UtcNow; }
                crisis.IsActive = true; crisis.UpdatedAt = DateTime.UtcNow;
            }
        }

        public async Task DeactivateCrisisAsync(int id)
        {
            var crisis = await _dbSet.FindAsync(id);
            if (crisis != null) { crisis.IsActive = false; crisis.SystemMode = SystemMode.Normal; crisis.UpdatedAt = DateTime.UtcNow; }
        }

        public async Task UpdateSystemModeAsync(int id, SystemMode mode)
        {
            var crisis = await _dbSet.FindAsync(id);
            if (crisis != null) { crisis.SystemMode = mode; crisis.UpdatedAt = DateTime.UtcNow; }
        }
    }
}