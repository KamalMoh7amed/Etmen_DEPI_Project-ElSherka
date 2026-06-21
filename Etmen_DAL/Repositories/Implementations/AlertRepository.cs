
namespace Etmen_DAL.Repositories.Implementations
{
    public class AlertRepository : GenericRepository<Alert>, IAlertRepository
    {
        public AlertRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<Alert>> GetByUserIdAsync(string userId)
            => await _dbSet.Where(a => a.UserId == userId).OrderByDescending(a => a.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Alert>> GetUnreadAlertsAsync(string userId)
            => await _dbSet.Where(a => a.UserId == userId && a.Status == AlertStatus.Unread).OrderByDescending(a => a.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Alert>> GetByTypeAsync(string userId, string type)
            => await _dbSet.Where(a => a.UserId == userId && a.AlertType == type).OrderByDescending(a => a.CreatedAt).ToListAsync();

        public async Task MarkAsReadAsync(int id)
        {
            var a = await _dbSet.FindAsync(id);
            if (a != null) { a.Status = AlertStatus.Read; a.ReadAt = DateTime.UtcNow; _dbSet.Update(a); }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var alerts = await _dbSet.Where(a => a.UserId == userId && a.Status == AlertStatus.Unread).ToListAsync();
            foreach (var a in alerts) { a.Status = AlertStatus.Read; a.ReadAt = DateTime.UtcNow; }
        }

        public async Task DismissAlertAsync(int id)
        {
            var a = await _dbSet.FindAsync(id);
            if (a != null) { a.Status = AlertStatus.Dismissed; _dbSet.Update(a); }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
            => await _dbSet.CountAsync(a => a.UserId == userId && a.Status == AlertStatus.Unread);

        public async Task<IEnumerable<Alert>> GetByDateRangeAsync(string userId, DateTime start, DateTime end)
            => await _dbSet.Where(a => a.UserId == userId && a.CreatedAt >= start && a.CreatedAt <= end).OrderByDescending(a => a.CreatedAt).ToListAsync();
    }
}