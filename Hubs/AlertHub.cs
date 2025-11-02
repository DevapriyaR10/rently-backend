using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Rently.Api.Models;

namespace Rently.Api.Hubs
{
    public class AlertHub : Hub
    {
        public async Task JoinTenantGroup(Guid tenantId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId.ToString());
        }

        public async Task LeaveTenantGroup(Guid tenantId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId.ToString());
        }

        public async Task SendAlertToTenant(Guid tenantId, Alert alert)
        {
            await Clients.Group(tenantId.ToString()).SendAsync("ReceiveAlert", alert);
        }
    }
}
