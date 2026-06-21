using Etmen_BLL.DTOs.Chat;


namespace Etmen_BLL.Repositories.IServices
{
    
    public interface IChatService
    {
        Task<ServiceResult<IEnumerable<ChatThreadDto>>> GetThreadsAsync(string userId);

        Task<ServiceResult<IEnumerable<ChatMessageDto>>> GetMessagesAsync(string senderId, string receiverId, int page = 1, int pageSize = 50);

        Task<ServiceResult<ChatMessageDto>> SendMessageAsync(string senderId, string receiverId, string content);

        Task<ServiceResult<bool>> MarkThreadReadAsync(string viewerUserId, string otherUserId);

        Task<ServiceResult<int>> GetUnreadCountAsync(string userId);
    }
}
