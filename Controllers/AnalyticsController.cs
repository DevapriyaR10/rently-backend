using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rently.Api.Data;
using Rently.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rently.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Summary: Total revenue, booking count by status
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] Guid tenantId)
        {
            var logs = await _context.AnalyticsLogs
                .Where(x => x.TenantId == tenantId)
                .ToListAsync();

            var totalRevenue = logs.Sum(x => x.Revenue);
            var totalBookings = logs.Count;
            var activeBookings = logs.Count(x => x.Status == "Active");
            var completedBookings = logs.Count(x => x.Status == "Completed");
            var cancelledBookings = logs.Count(x => x.Status == "Cancelled");

            var summary = new
            {
                totalRevenue,
                totalBookings,
                activeBookings,
                completedBookings,
                cancelledBookings
            };

            return Ok(summary);
        }

        // ✅ Revenue Trend (last 7 days)
        [HttpGet("revenue-trend")]
        public async Task<IActionResult> GetRevenueTrend([FromQuery] Guid tenantId)
        {
            var trend = await _context.AnalyticsLogs
                .Where(x => x.TenantId == tenantId && x.LoggedAt >= DateTime.UtcNow.AddDays(-7))
                .GroupBy(x => x.LoggedAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    revenue = g.Sum(x => x.Revenue)
                })
                .OrderBy(g => g.date)
                .ToListAsync();

            return Ok(trend);
        }

        // ✅ Booking Trend (last 7 days)
        [HttpGet("booking-trend")]
        public async Task<IActionResult> GetBookingTrend([FromQuery] Guid tenantId)
        {
            var trend = await _context.AnalyticsLogs
                .Where(x => x.TenantId == tenantId && x.LoggedAt >= DateTime.UtcNow.AddDays(-7))
                .GroupBy(x => x.LoggedAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count()
                })
                .OrderBy(g => g.date)
                .ToListAsync();

            return Ok(trend);
        }

        // ✅ Live Recent Logs (for real-time section)
        [HttpGet("all")]
        public async Task<IActionResult> GetAllLogs([FromQuery] Guid tenantId)
        {
            var logs = await _context.AnalyticsLogs
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.LoggedAt)
                .Take(20)
                .Select(x => new
                {
                    x.CustomerName,
                    x.Category,
                    x.Condition,
                    x.Price,
                    x.Revenue,
                    x.Status,
                    x.LoggedAt
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
