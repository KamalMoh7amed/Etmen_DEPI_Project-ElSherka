using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class ChatMessageRepository : GenericRepository<ChatMessage>, IChatMessageRepository
    {
        public ChatMessageRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<ChatMessage>> GetByConversationAsync(string u1, string u2)
            => await _dbSet.Include(m => m.Sender).Include(m => m.Receiver)
                           .Where(m => (m.SenderId == u1 && m.ReceiverId == u2) || (m.SenderId == u2 && m.ReceiverId == u1)).OrderBy(m => m.SentAt).ToListAsync();

        public async Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string receiver, string sender)
            => await _dbSet.Where(m => m.ReceiverId == receiver && m.SenderId == sender && !m.IsRead).OrderBy(m => m.SentAt).ToListAsync();

        public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(string userId, int count)
            => await _dbSet.Include(m => m.Sender).Include(m => m.Receiver)
                           .Where(m => m.SenderId == userId || m.ReceiverId == userId).OrderByDescending(m => m.SentAt).Take(count).ToListAsync();

        public async Task MarkAsReadAsync(string receiver, string sender)
        {
            await _dbSet.Where(m => m.ReceiverId == receiver && m.SenderId == sender && !m.IsRead)
                        .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
        }

        public async Task<int> GetUnreadCountAsync(string userId)
            => await _dbSet.CountAsync(m => m.ReceiverId == userId && !m.IsRead);

        public async Task<IEnumerable<ChatMessage>> GetMessagesByDateRangeAsync(string u1, string u2, DateTime start, DateTime end)
            => await _dbSet.Where(m => ((m.SenderId == u1 && m.ReceiverId == u2) || (m.SenderId == u2 && m.ReceiverId == u1)) && m.SentAt >= start && m.SentAt <= end).OrderBy(m => m.SentAt).ToListAsync();
    }
}