using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Etmen_PL.Hubs
{
    public class ChatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            // Optionally track online users or log connection
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendTyping(string receiverId, bool isTyping)
        {
            var senderId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(senderId) && !string.IsNullOrEmpty(receiverId))
            {
                await Clients.User(receiverId).SendAsync("ReceiveTyping", senderId, isTyping);
            }
        }
    }
}
