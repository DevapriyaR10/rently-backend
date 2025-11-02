using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Rently.Api.Hubs
{
    public class AnalyticsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        // ✅ Add this method
        public async Task JoinTenantGroup(string tenantId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        }

        // ✅ Optional: method to send updates to all clients in a group
        public async Task SendAnalyticsUpdate(string tenantId, object data)
        {
            await Clients.Group(tenantId).SendAsync("ReceiveAnalyticsUpdate", data);
        }
    }
}
