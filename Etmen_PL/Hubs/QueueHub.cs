using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Etmen_PL.Hubs
{
    public class QueueHub : Hub
    {
        public async Task JoinProviderGroup(string providerId)
        {
            if (!string.IsNullOrEmpty(providerId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Provider_{providerId}");
            }
        }

        public async Task LeaveProviderGroup(string providerId)
        {
            if (!string.IsNullOrEmpty(providerId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Provider_{providerId}");
            }
        }

        public async Task SendInternalChatMessage(int providerId, string senderName, string messageText)
        {
            await Clients.Group($"Provider_{providerId}").SendAsync("ReceiveInternalChatMessage", senderName, messageText, System.DateTime.UtcNow.AddHours(3).ToString("HH:mm"));
        }

        public async Task ActivateEmergencyCode(int providerId, string senderName, string codeName, string locationDetails)
        {
            await Clients.Group($"Provider_{providerId}").SendAsync("ReceiveEmergencyCode", senderName, codeName, locationDetails);
        }
    }
}
