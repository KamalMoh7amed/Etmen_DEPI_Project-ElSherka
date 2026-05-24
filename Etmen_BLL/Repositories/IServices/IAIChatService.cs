using Etmen_BLL.DTOs.Chat;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    public interface IAIChatService
    {
        Task<ServiceResult<ChatMessageDto>> SendMessageAsync(int userId, string message);
        Task<ServiceResult<ChatThreadDto>> GetChatThreadAsync(int userId);
        Task<ServiceResult<List<ChatMessageDto>>> GetChatHistoryAsync(int userId, int pageNumber = 1, int pageSize = 20);
        Task<ServiceResult> ClearChatHistoryAsync(int userId);
    }
}
