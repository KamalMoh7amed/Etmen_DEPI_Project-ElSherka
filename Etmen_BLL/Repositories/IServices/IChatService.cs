using Etmen_BLL.DTOs.Chat;
using Etmen_BLL.Helpers;

namespace Etmen_BLL.Repositories.IServices
{
    /// <summary>
    /// Handles real-time and async messaging between Patients and Doctors.
    /// US-14, US-15, US-16
    /// </summary>
    public interface IChatService
    {
        /// <summary>Returns all conversation threads for the given user.</summary>
        Task<ServiceResult<IEnumerable<ChatThreadDto>>> GetThreadsAsync(string userId);

        /// <summary>Returns paginated messages for a specific thread between two users.</summary>
        Task<ServiceResult<IEnumerable<ChatMessageDto>>> GetMessagesAsync(string senderId, string receiverId, int page = 1, int pageSize = 50);

        /// <summary>Persists a new chat message and triggers a Notification for the recipient.</summary>
        Task<ServiceResult<ChatMessageDto>> SendMessageAsync(string senderId, string receiverId, string content);

        /// <summary>Marks all unread messages in a thread as read.</summary>
        Task<ServiceResult<bool>> MarkThreadReadAsync(string viewerUserId, string otherUserId);

        /// <summary>Returns unread message count for the given user (used by notification bell).</summary>
        Task<ServiceResult<int>> GetUnreadCountAsync(string userId);
    }
}
