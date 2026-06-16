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
    }
}
