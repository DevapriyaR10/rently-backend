using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Rently.Api.Data;
using Rently.Api.Models;
using Rently.Api.Hubs;
using Rently.Api.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rently.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<AnalyticsHub> _hubContext;
        private readonly AlertService _alertService;

        public BookingController(AppDbContext context, IHubContext<AnalyticsHub> hubContext, AlertService alertService)
        {
            _context = context;
            _hubContext = hubContext;
            _alertService = alertService;
        }

        // 📅 GET all bookings for a tenant
        [HttpGet]
        public async Task<IActionResult> GetBookings([FromQuery] string tenantId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out Guid tenantGuid))
                    return BadRequest("Invalid or missing tenantId.");

                var bookings = await _context.Bookings
                    .Include(b => b.InventoryItem)
                    .Include(b => b.Customer)
                    .Where(b => b.TenantId == tenantGuid)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new
                    {
                        b.BookingId,
                        b.Status,
                        b.StartDate,
                        b.EndDate,
                        b.CreatedAt,
                        b.CustomerId,
                        b.CustomerName,
                        b.CustomerEmail,
                        Customer = new
                        {
                            b.Customer.CustomerId,
                            b.Customer.FullName,
                            b.Customer.Email,
                            b.Customer.Phone,
                            b.Customer.Address,
                            b.Customer.IdDocumentUrl
                        },
                        InventoryItem = new
                        {
                            b.InventoryItem.ItemId,
                            b.InventoryItem.Name,
                            b.InventoryItem.Category,
                            b.InventoryItem.Condition,
                            b.InventoryItem.Status,
                            b.InventoryItem.ImageUrl,
                            b.InventoryItem.TimesRented,
                            b.InventoryItem.Price
                        }
                    })
                    .ToListAsync();

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, $"Error fetching bookings: {ex.Message}");
            }
        }

        // 📥 DTOs
        public class CreateBookingDto
        {
            public Guid TenantId { get; set; }
            public Guid InventoryItemId { get; set; }
            public string CustomerEmail { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public class UpdateBookingDto
        {
            public string? CustomerName { get; set; }
            public string? CustomerEmail { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string? Status { get; set; }
        }

        // 🔹 Helper: Log Analytics Data
        private async Task LogAnalyticsAsync(Guid tenantId, Booking booking)
        {
            try
            {
                var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ItemId == booking.InventoryItemId);
                if (item == null) return;

                var totalDays = Math.Max(1, (booking.EndDate - booking.StartDate).TotalDays);
                var revenue = item.Price * (decimal)totalDays;

                var existingLog = await _context.AnalyticsLogs.FirstOrDefaultAsync(a => a.BookingId == booking.BookingId);
                if (existingLog != null)
                {
                    existingLog.CustomerName = booking.CustomerName;
                    existingLog.Category = item.Category;
                    existingLog.Condition = item.Condition;
                    existingLog.Price = item.Price;
                    existingLog.Revenue = revenue;
                    existingLog.Status = booking.Status;
                    existingLog.StartDate = booking.StartDate;
                    existingLog.EndDate = booking.EndDate;
                    existingLog.LoggedAt = DateTime.UtcNow;

                    _context.AnalyticsLogs.Update(existingLog);
                }
                else
                {
                    await _context.AnalyticsLogs.AddAsync(new AnalyticsLog
                    {
                        TenantId = tenantId,
                        BookingId = booking.BookingId,
                        CustomerName = booking.CustomerName,
                        Category = item.Category,
                        Condition = item.Condition,
                        Price = item.Price,
                        Revenue = revenue,
                        Status = booking.Status,
                        StartDate = booking.StartDate,
                        EndDate = booking.EndDate,
                        LoggedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LogAnalytics error: {ex.Message}");
            }
        }

        // ➕ Create Booking + Rental
        [HttpPost]
        [Authorize(Roles = "admin,manager,staff")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.CustomerEmail))
                    return BadRequest("Invalid booking data.");

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == dto.CustomerEmail.ToLower()
                        && c.TenantId == dto.TenantId);
                if (customer == null)
                    return BadRequest("Customer not found. Please add the customer first.");

                var item = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.ItemId == dto.InventoryItemId && i.TenantId == dto.TenantId);
                if (item == null)
                    return NotFound("Inventory item not found.");
                if (item.Status == "Rented" || item.Status == "Reserved")
                    return BadRequest("Item is not available for booking.");

                var booking = new Booking
                {
                    BookingId = Guid.NewGuid(),
                    TenantId = dto.TenantId,
                    InventoryItemId = item.ItemId,
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.FullName,
                    CustomerEmail = customer.Email,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Reserved"
                };

                _context.Bookings.Add(booking);
                item.Status = "Reserved";
                await _context.SaveChangesAsync();

                // 🏷️ Create corresponding Rental
                var rental = new Rental
                {
                    TenantId = dto.TenantId,
                    ItemId = item.ItemId,
                    CustomerId = customer.CustomerId,
                    StartDate = dto.StartDate,
                    DueDate = dto.EndDate,
                    Status = "Active",
                    Price = item.Price,
                    AmountPaid = 0,
                    OutstandingAmount = item.Price * (decimal)Math.Max(1, (dto.EndDate - dto.StartDate).TotalDays)
                };

                _context.Rentals.Add(rental);
                await _context.SaveChangesAsync();

                // 📊 Log Analytics
                await LogAnalyticsAsync(dto.TenantId, booking);

                // 🚀 Notify dashboards
                await _hubContext.Clients.Group(dto.TenantId.ToString()).SendAsync("AnalyticsUpdated", new
                {
                    tenantId = dto.TenantId,
                    bookingId = booking.BookingId,
                    action = "created",
                    status = booking.Status,
                    time = DateTime.UtcNow
                });

                // 🔔 Send instant alert
                await _alertService.CreateImmediateAlertAsync(dto.TenantId, "BookingCreated",
                    $"A new booking was created for {booking.CustomerName} ({item.Name}) from {booking.StartDate:dd MMM} to {booking.EndDate:dd MMM}. Rental record created automatically.");

                return Ok(new
                {
                    Message = "Booking and rental created successfully.",
                    Booking = booking,
                    Rental = rental
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, $"Error creating booking: {ex.Message}");
            }
        }

        // ✏️ Update Booking
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> UpdateBooking(Guid id, [FromBody] UpdateBookingDto dto)
        {
            try
            {
                var booking = await _context.Bookings.Include(b => b.InventoryItem)
                    .FirstOrDefaultAsync(b => b.BookingId == id);
                if (booking == null)
                    return NotFound("Booking not found.");

                booking.CustomerName = dto.CustomerName ?? booking.CustomerName;
                booking.CustomerEmail = dto.CustomerEmail ?? booking.CustomerEmail;
                booking.StartDate = dto.StartDate ?? booking.StartDate;
                booking.EndDate = dto.EndDate ?? booking.EndDate;
                booking.Status = dto.Status ?? booking.Status;

                switch (booking.Status)
                {
                    case "Active":
                        booking.InventoryItem.Status = "Rented";
                        break;
                    case "Completed":
                        booking.InventoryItem.Status = "Available";
                        booking.InventoryItem.TimesRented += 1;
                        break;
                    case "Cancelled":
                        booking.InventoryItem.Status = "Available";
                        break;
                }

                await _context.SaveChangesAsync();
                await LogAnalyticsAsync(booking.TenantId, booking);

                await _hubContext.Clients.Group(booking.TenantId.ToString()).SendAsync("AnalyticsUpdated", new
                {
                    tenantId = booking.TenantId,
                    bookingId = booking.BookingId,
                    action = "updated",
                    status = booking.Status,
                    time = DateTime.UtcNow
                });

                // 🔔 Alerts
                if (booking.Status == "Completed")
                {
                    await _alertService.CreateImmediateAlertAsync(booking.TenantId, "BookingCompleted",
                        $"Booking for {booking.CustomerName} has been completed successfully.");
                }
                else if (booking.Status == "Cancelled")
                {
                    await _alertService.CreateImmediateAlertAsync(booking.TenantId, "BookingCancelled",
                        $"Booking for {booking.CustomerName} was cancelled.");
                }

                return Ok(new { Message = "Booking updated successfully.", Booking = booking });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, $"Error updating booking: {ex.Message}");
            }
        }

        // ❌ Delete Booking
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteBooking(Guid id)
        {
            try
            {
                var booking = await _context.Bookings.Include(b => b.InventoryItem)
                    .FirstOrDefaultAsync(b => b.BookingId == id);
                if (booking == null)
                    return NotFound("Booking not found.");

                if (booking.InventoryItem != null)
                {
                    booking.InventoryItem.Status = "Available";
                    _context.InventoryItems.Update(booking.InventoryItem);
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group(booking.TenantId.ToString()).SendAsync("AnalyticsUpdated", new
                {
                    tenantId = booking.TenantId,
                    bookingId = booking.BookingId,
                    action = "deleted",
                    time = DateTime.UtcNow
                });

                await _alertService.CreateImmediateAlertAsync(booking.TenantId, "BookingDeleted",
                    $"Booking for {booking.CustomerName} has been deleted.");

                return Ok(new { Message = "Booking deleted successfully.", DeletedBookingId = id });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, $"Error deleting booking: {ex.Message}");
            }
        }
    }
}
