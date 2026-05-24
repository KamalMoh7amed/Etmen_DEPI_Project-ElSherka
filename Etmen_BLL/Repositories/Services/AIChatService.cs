using Etmen_BLL.DTOs.Chat;
using Etmen_BLL.Helpers;
using Etmen_BLL.Repositories.IServices;
using Etmen_DAL.Repositories.Interfaces;

namespace Etmen_BLL.Repositories.Services
{
    public sealed class AIChatService : IAIChatService
    {
        private readonly IUnitOfWork _uow;

        public AIChatService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<ServiceResult<ChatMessageDto>> SendMessageAsync(int userId, string message)
        {
            throw new NotImplementedException("SendMessageAsync is not implemented yet.");
        }

        public Task<ServiceResult<ChatThreadDto>> GetChatThreadAsync(int userId)
        {
            throw new NotImplementedException("GetChatThreadAsync is not implemented yet.");
        }

        public Task<ServiceResult<List<ChatMessageDto>>> GetChatHistoryAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            throw new NotImplementedException("GetChatHistoryAsync is not implemented yet.");
        }

        public Task<ServiceResult> ClearChatHistoryAsync(int userId)
        {
            throw new NotImplementedException("ClearChatHistoryAsync is not implemented yet.");
        }

    }
}