using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_DAL.Helpers;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class OutbreakZoneRepository : GenericRepository<OutbreakZone>, IOutbreakZoneRepository
    {
        public OutbreakZoneRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<OutbreakZone>> GetByCrisisIdAsync(int crisisId)
            => await _dbSet.Where(z => z.CrisisConfigurationId == crisisId).ToListAsync();

        public async Task<IEnumerable<OutbreakZone>> GetNearbyZonesAsync(decimal lat, decimal lon, decimal radiusKm)
        {
            var zones = await _dbSet.ToListAsync();
            return zones.Where(z => GeoHelper.CalculateDistance(lat, lon, z.CenterLatitude, z.CenterLongitude) <= (z.RadiusInKm + radiusKm));
        }

        public async Task<IEnumerable<OutbreakZone>> GetActiveZonesAsync(int crisisId)
            => await _dbSet.Where(z => z.CrisisConfigurationId == crisisId).ToListAsync();

        public async Task<bool> IsPointInZoneAsync(decimal lat, decimal lon, int zoneId)
        {
            var zone = await _dbSet.FindAsync(zoneId);
            return zone != null && GeoHelper.IsPointInZone(lat, lon, zone);
        }

        public async Task<IEnumerable<OutbreakZone>> GetZonesByRiskLevelAsync(int crisisId, int level)
            => await _dbSet.Where(z => z.CrisisConfigurationId == crisisId && z.RiskLevel == level).ToListAsync();

        public async Task UpdateZoneRiskLevelAsync(int id, int level)
        {
            var zone = await _dbSet.FindAsync(id);
            if (zone != null) { zone.RiskLevel = level; _dbSet.Update(zone); }
        }
    }
}