using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Etmen_PL.Hubs
{
    /// <summary>
    /// Real-time hub for Emergency Panic Inbox.
    /// Doctors join a shared "Doctors" group so that case assignments
    /// broadcast immediately across all connected doctor sessions.
    /// </summary>
    [Authorize]
    public class EmergencyHub : Hub
    {
        private const string DoctorsGroup = "AllDoctors";

        public override async Task OnConnectedAsync()
        {
            // If the user is a doctor, join the doctors broadcast group
            if (Context.User != null && Context.User.IsInRole("Doctor"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, DoctorsGroup);
                var userId = Context.UserIdentifier;
                if (!string.IsNullOrEmpty(userId))
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Doctor_{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User != null && Context.User.IsInRole("Doctor"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, DoctorsGroup);
                var userId = Context.UserIdentifier;
                if (!string.IsNullOrEmpty(userId))
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Doctor_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a specific patient's emergency group to receive real-time updates (for patient and their family).
        /// </summary>
        public async Task JoinPatientEmergencyGroup(string patientUserId)
        {
            if (!string.IsNullOrEmpty(patientUserId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Emergency_{patientUserId}");
            }
        }

        /// <summary>
        /// Leave a specific patient's emergency group.
        /// </summary>
        public async Task LeavePatientEmergencyGroup(string patientUserId)
        {
            if (!string.IsNullOrEmpty(patientUserId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Emergency_{patientUserId}");
            }
        }

        /// <summary>
        /// Called by a doctor client to confirm they are live on the inbox page.
        /// </summary>
        public async Task Ping() =>
            await Clients.Caller.SendAsync("Pong");
    }
}
