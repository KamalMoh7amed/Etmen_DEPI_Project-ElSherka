
namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IOutbreakZoneRepository : IGenericRepository<OutbreakZone>
    {
        Task<IEnumerable<OutbreakZone>> GetByCrisisIdAsync(int crisisId);
        Task<IEnumerable<OutbreakZone>> GetNearbyZonesAsync(decimal latitude, decimal longitude, decimal radiusInKm);
        Task<IEnumerable<OutbreakZone>> GetActiveZonesAsync(int crisisId);
        Task<bool> IsPointInZoneAsync(decimal latitude, decimal longitude, int zoneId);
        Task<IEnumerable<OutbreakZone>> GetZonesByRiskLevelAsync(int crisisId, int riskLevel);
        Task UpdateZoneRiskLevelAsync(int zoneId, int newRiskLevel);
    }
}