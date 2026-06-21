
namespace Etmen_BLL.Repositories.Services
{
    /// <summary>
    /// Manages patient–doctor chat threads.
    /// Stores messages via IChatMessageRepository and raises Notifications for recipients.
    /// US-14 (AI context), US-15 (health record context), US-16 (doctor view).
    /// </summary>
    public sealed class ChatService : IChatService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IUnitOfWork uow, ILogger<ChatService> logger)
        {
            _uow    = uow;
            _logger = logger;
        }

        /// <summary>
        /// Returns all unique conversation partners for the given user, with last message info.
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ChatThreadDto>>> GetThreadsAsync(string userId)
        {
            try
            {
                var messages = await _uow.ChatMessages.GetRecentMessagesAsync(userId, int.MaxValue);
                if (messages == null || !messages.Any())
                {
                    return ServiceResult<IEnumerable<ChatThreadDto>>.Success(
                        new List<ChatThreadDto>());
                }

                // Group messages by conversation partner to build unique threads
                var threads = new Dictionary<string, ChatThreadDto>();

                foreach (var msg in messages)
                {
                    string otherUserId = msg.SenderId == userId ? msg.ReceiverId : msg.SenderId;
                    var partner = msg.SenderId == userId ? msg.Receiver : msg.Sender;
                    string otherUserName = partner != null 
                        ? (!string.IsNullOrEmpty(partner.FirstName) ? $"{partner.FirstName} {partner.LastName}".Trim() : partner.UserName ?? "Unknown")
                        : "Unknown";

                    if (threads.ContainsKey(otherUserId))
                    {
                        // Update if this message is more recent
                        if (msg.SentAt > threads[otherUserId].LastMessageAt)
                        {
                            threads[otherUserId].LastMessage = msg.Message;
                            threads[otherUserId].LastMessageAt = msg.SentAt;
                        }
                        // Update unread count if message is unread and for this user
                        if (!msg.IsRead && msg.ReceiverId == userId)
                        {
                            threads[otherUserId].UnreadCount++;
                        }
                    }
                    else
                    {
                        threads[otherUserId] = new ChatThreadDto
                        {
                            UserId = otherUserId,
                            UserName = otherUserName,
                            LastMessage = msg.Message,
                            LastMessageAt = msg.SentAt,
                            UnreadCount = (!msg.IsRead && msg.ReceiverId == userId) ? 1 : 0
                        };
                    }
                }

                return ServiceResult<IEnumerable<ChatThreadDto>>.Success(
                    threads.Values.OrderByDescending(t => t.LastMessageAt));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving chat threads for user {userId}: {ex.Message}");
                return ServiceResult<IEnumerable<ChatThreadDto>>.Failure(
                    $"Failed to retrieve chat threads: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns paginated messages for a specific thread between two users.
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ChatMessageDto>>> GetMessagesAsync(
            string senderId, string receiverId, int page = 1, int pageSize = 50)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;

                var messages = await _uow.ChatMessages.GetByConversationAsync(senderId, receiverId);
                if (messages == null)
                {
                    return ServiceResult<IEnumerable<ChatMessageDto>>.Success(
                        new List<ChatMessageDto>());
                }

                // Sort by date (descending) to get the latest messages first, then apply pagination
                var sorted = messages.OrderByDescending(m => m.SentAt).ToList();
                var paginated = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                // Reverse the page to lay them out chronologically (ascending) in the UI
                paginated.Reverse();

                var dtos = paginated.Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender != null && !string.IsNullOrEmpty(m.Sender.FirstName) ? $"{m.Sender.FirstName} {m.Sender.LastName}".Trim() : m.Sender?.UserName ?? "Unknown",
                    ReceiverId = m.ReceiverId,
                    Message = m.Message,
                    IsRead = m.IsRead,
                    SentAt = m.SentAt
                }).ToList();

                return ServiceResult<IEnumerable<ChatMessageDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving messages between {senderId} and {receiverId}: {ex.Message}");
                return ServiceResult<IEnumerable<ChatMessageDto>>.Failure(
                    $"Failed to retrieve messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Persists a new chat message and triggers a Notification for the recipient.
        /// </summary>
        public async Task<ServiceResult<ChatMessageDto>> SendMessageAsync(
            string senderId, string receiverId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return ServiceResult<ChatMessageDto>.Failure("Message content cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
                {
                    return ServiceResult<ChatMessageDto>.Failure("Sender and receiver IDs are required.");
                }

                // Create new ChatMessage entity
                var chatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Message = content,
                    IsRead = false,
                    SentAt = DateTime.UtcNow
                };

                // Add message to repository
                await _uow.ChatMessages.AddAsync(chatMessage);
                await _uow.CompleteAsync();

                var dto = new ChatMessageDto
                {
                    Id = chatMessage.Id,
                    SenderId = chatMessage.SenderId,
                    SenderName = chatMessage.Sender?.UserName ?? "Unknown",
                    ReceiverId = chatMessage.ReceiverId,
                    Message = chatMessage.Message,
                    IsRead = chatMessage.IsRead,
                    SentAt = chatMessage.SentAt
                };

                _logger.LogInformation($"Message sent from {senderId} to {receiverId} with ID {chatMessage.Id}");

                return ServiceResult<ChatMessageDto>.Created(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message from {senderId} to {receiverId}: {ex.Message}");
                return ServiceResult<ChatMessageDto>.Failure(
                    $"Failed to send message: {ex.Message}");
            }
        }

        /// <summary>
        /// Marks all unread messages in a thread as read.
        /// </summary>
        public async Task<ServiceResult<bool>> MarkThreadReadAsync(string viewerUserId, string otherUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(viewerUserId) || string.IsNullOrWhiteSpace(otherUserId))
                {
                    return ServiceResult<bool>.Failure("User IDs are required.");
                }

                await _uow.ChatMessages.MarkAsReadAsync(viewerUserId, otherUserId);
                await _uow.CompleteAsync();

                _logger.LogInformation($"Marked messages as read for {viewerUserId} from {otherUserId}");

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking thread read for {viewerUserId} and {otherUserId}: {ex.Message}");
                return ServiceResult<bool>.Failure(
                    $"Failed to mark thread as read: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns unread message count for the given user (used by notification bell).
        /// </summary>
        public async Task<ServiceResult<int>> GetUnreadCountAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResult<int>.Failure("User ID is required.");
                }

                var unreadCount = await _uow.ChatMessages.GetUnreadCountAsync(userId);

                return ServiceResult<int>.Success(unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving unread count for {userId}: {ex.Message}");
                return ServiceResult<int>.Failure(
                    $"Failed to retrieve unread count: {ex.Message}");
            }
        }
    }
}
