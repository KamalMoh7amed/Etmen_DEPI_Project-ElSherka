namespace Etmen_DAL.Repositories.Implementations
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId)
            => await _dbSet.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId)
            => await _dbSet.Where(n => n.UserId == userId && !n.IsRead).OrderByDescending(n => n.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Notification>> GetLatestNotificationsAsync(string userId, int count)
            => await _dbSet.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).Take(count).ToListAsync();

        public async Task MarkAsReadAsync(int id)
        {
            var n = await _dbSet.FindAsync(id);
            if (n != null) { n.IsRead = true; _dbSet.Update(n); }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifs = await _dbSet.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in notifs) n.IsRead = true;
        }

        public async Task<int> GetUnreadCountAsync(string userId)
            => await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task DeleteNotificationAsync(int id, string userId)
        {
            var n = await _dbSet.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (n != null) _dbSet.Remove(n);
        }
    }
}