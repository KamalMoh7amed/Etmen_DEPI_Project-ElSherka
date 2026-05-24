using Etmen_Domain.Entities;

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IChatMessageRepository : IGenericRepository<ChatMessage>
    {
        Task<IEnumerable<ChatMessage>> GetByConversationAsync(string userId1, string userId2);
        Task<IEnumerable<ChatMessage>> GetUnreadMessagesAsync(string receiverId, string senderId);
        Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(string userId, int count);
        Task MarkAsReadAsync(string receiverId, string senderId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<ChatMessage>> GetMessagesByDateRangeAsync(string userId1, string userId2, DateTime startDate, DateTime endDate);
    }
}