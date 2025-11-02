using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Rently.Api.Data;
using Rently.Api.Hubs;
using Rently.Api.Models;

namespace Rently.Api.Services
{
    public class AlertService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<AlertHub> _hubContext;

        public AlertService(AppDbContext context, IHubContext<AlertHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 🔹 Event-Driven: Trigger instantly when something happens (e.g., booking created)
        public async Task CreateImmediateAlertAsync(Guid tenantId, string type, string message)
        {
            var alert = new Alert
            {
                TenantId = tenantId,
                Type = type,
                Message = message
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            // 🔔 Push real-time alert to connected tenant clients
            await _hubContext.Clients.Group(tenantId.ToString())
                .SendAsync("ReceiveAlert", alert);
        }

        // 🔹 Scheduled: Run daily or hourly to check conditions
        public async Task GenerateAlertsAsync()
        {
            var now = DateTime.UtcNow;
            var tomorrow = now.AddDays(1);

            // Optional cleanup of old alerts (older than 30 days)
            var oldAlerts = _context.Alerts.Where(a => a.CreatedAt < now.AddDays(-30));
            _context.Alerts.RemoveRange(oldAlerts);

            // --- Pending Payments ---
            var pendingPayments = await _context.Payments
                .Where(p => p.Status != "Completed")
                .ToListAsync();

            foreach (var pay in pendingPayments)
            {
                await CreateImmediateAlertAsync(pay.TenantId, "PendingPayment",
                    $"Payment of ₹{pay.Amount} for rental {pay.RentalId} is still pending.");
            }

            // --- Upcoming Bookings ---
            var upcomingBookings = await _context.Bookings
                .Where(b => b.StartDate > now && b.StartDate <= tomorrow)
                .ToListAsync();

            foreach (var booking in upcomingBookings)
            {
                await CreateImmediateAlertAsync(booking.TenantId, "UpcomingBooking",
                    $"Booking for {booking.CustomerName} starts tomorrow ({booking.StartDate:dd MMM}).");
            }

            // --- Upcoming Returns ---
            var upcomingReturns = await _context.Bookings
                .Where(b => b.EndDate > now && b.EndDate <= tomorrow)
                .ToListAsync();

            foreach (var booking in upcomingReturns)
            {
                await CreateImmediateAlertAsync(booking.TenantId, "UpcomingReturn",
                    $"Return due for {booking.CustomerName} on {booking.EndDate:dd MMM}.");
            }

            // --- Maintenance Items ---
            var maintenanceItems = await _context.InventoryItems
                .Where(i => i.Status == "Maintenance")
                .ToListAsync();

            foreach (var item in maintenanceItems)
            {
                await CreateImmediateAlertAsync(item.TenantId, "MaintenanceItem",
                    $"{item.Name} is currently under maintenance.");
            }
        }
    }
}
