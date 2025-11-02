using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Rently.Api.Data;
using Rently.Api.Hubs;
using Rently.Api.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Rently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<AlertHub> _hubContext;

        public AlertsController(AppDbContext context, IHubContext<AlertHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ✅ Send alert + store in DB
        [HttpPost("send")]
        public async Task<IActionResult> SendAlert([FromBody] AlertRequest request)
        {
            if (request == null || request.TenantId == Guid.Empty)
                return BadRequest("Invalid request.");

            var alert = new Alert
            {
                TenantId = request.TenantId,
                Type = request.Type ?? "General",
                Message = request.Message ?? "New notification",
                CreatedAt = DateTime.UtcNow
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(request.TenantId.ToString())
                .SendAsync("ReceiveAlert", new
                {
                    type = alert.Type,
                    message = alert.Message,
                    createdAt = alert.CreatedAt
                });

            return Ok(new { success = true, saved = alert });
        }

        // ✅ Get all alerts for a tenant
        [HttpGet("{tenantId}")]
        public async Task<IActionResult> GetAlerts(Guid tenantId)
        {
            var alerts = await _context.Alerts
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(alerts);
        }

        // ✅ Mark alert as read
        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null) return NotFound();

            alert.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Alert marked as read" });
        }

        // ✅ Clear all alerts for a tenant
[HttpDelete("clear/{tenantId}")]
public async Task<IActionResult> ClearAlerts(Guid tenantId)
{
    var alerts = _context.Alerts.Where(a => a.TenantId == tenantId);
    if (!alerts.Any())
        return NotFound(new { message = "No alerts found for tenant." });

    _context.Alerts.RemoveRange(alerts);
    await _context.SaveChangesAsync();

    return Ok(new { message = "All alerts cleared successfully." });
}


        public class AlertRequest
        {
            public Guid TenantId { get; set; }
            public string Type { get; set; }
            public string Message { get; set; }
        }
    }
}
