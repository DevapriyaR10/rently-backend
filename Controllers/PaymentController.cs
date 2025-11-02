using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Rently.Api.Data;
using Rently.Api.Models;
using Rently.Api.Services;
using Rently.Api.Hubs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Rently.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly InvoiceService _invoiceService;
        private readonly IBlobService _blobService;
        private readonly IHubContext<AlertHub> _hubContext;

        public PaymentController(
            AppDbContext context,
            InvoiceService invoiceService,
            AzureBlobService blobService,
            IHubContext<AlertHub> hubContext)
        {
            _context = context;
            _invoiceService = invoiceService;
            _blobService = blobService;
            _hubContext = hubContext;
        }

        // ✅ Create a new payment and generate invoice
        [HttpPost("create")]
        [Authorize(Roles = "admin,manager,staff")]
        public async Task<IActionResult> CreatePayment([FromBody] Payment payment)
        {
            var rental = await _context.Rentals.FirstOrDefaultAsync(r => r.Id == payment.RentalId);
            if (rental == null)
                return NotFound("Rental not found");

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Generate invoice PDF
            var pdfBytes = await _invoiceService.GenerateInvoiceAsync(payment);
            var fileName = $"invoice_{payment.PaymentId}.pdf";
            var containerName = "invoices";

            using var stream = new MemoryStream(pdfBytes);
            var invoiceUrl = await _blobService.UploadAsync(containerName, stream, fileName);

            // Update payment record
            payment.InvoiceUrl = invoiceUrl;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            // 🚀 Real-time alert to tenant group
            await _hubContext.Clients.Group(payment.TenantId.ToString())
                .SendAsync("ReceiveAlert", new
                {
                    type = "success",
                    title = "New Payment",
                    message = $"A new payment of ₹{payment.Amount} has been recorded for rental #{payment.RentalId}.",
                    time = DateTime.UtcNow
                });

            return Ok(new
            {
                message = "Payment created successfully",
                paymentId = payment.PaymentId,
                invoiceUrl
            });
        }

        // ✅ View or Download invoice
        [HttpGet("invoice/{paymentId}")]
        [Authorize(Roles = "admin,manager,staff,tenant")]
        public async Task<IActionResult> GetInvoice(Guid paymentId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null)
                return NotFound("Payment not found");

            if (string.IsNullOrEmpty(payment.InvoiceUrl))
                return NotFound("Invoice not generated yet.");

            var uri = new Uri(payment.InvoiceUrl);
            var containerName = uri.Segments[1].TrimEnd('/');
            var fileName = uri.Segments[^1];

            var sasUrl = _blobService.GenerateReadSasUrl(containerName, fileName, 15);

            return Ok(new { sasUrl });
        }

        // ✅ List all payments
        [Authorize(Roles = "admin,manager,staff")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.Rental)
                .ToListAsync();
            return Ok(payments);
        }

        // ✅ Edit/Update Payment (Broadcast alert)
        [Authorize(Roles = "admin,manager")]
        [HttpPut("edit/{paymentId}")]
        public async Task<IActionResult> EditPayment(Guid paymentId, [FromBody] Payment updatedPayment)
        {
            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (existingPayment == null)
                return NotFound("Payment not found.");

            existingPayment.Amount = updatedPayment.Amount;
            existingPayment.PaymentMethod = updatedPayment.PaymentMethod;
            existingPayment.Status = updatedPayment.Status;
            existingPayment.Notes = updatedPayment.Notes;

            _context.Payments.Update(existingPayment);
            await _context.SaveChangesAsync();

            // 🚀 Real-time alert
            await _hubContext.Clients.Group(existingPayment.TenantId.ToString())
                .SendAsync("ReceiveAlert", new
                {
                    type = "info",
                    title = "Payment Updated",
                    message = $"Payment #{existingPayment.PaymentId} has been updated. Status: {existingPayment.Status}",
                    time = DateTime.UtcNow
                });

            return Ok(new
            {
                message = "Payment updated successfully.",
                payment = existingPayment
            });
        }

        // ✅ Delete Payment (Broadcast alert)
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("delete/{paymentId}")]
        public async Task<IActionResult> DeletePayment(Guid paymentId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null)
                return NotFound("Payment not found.");

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            // 🚀 Real-time alert
            await _hubContext.Clients.Group(payment.TenantId.ToString())
                .SendAsync("ReceiveAlert", new
                {
                    type = "warning",
                    title = "Payment Deleted",
                    message = $"Payment record #{paymentId} has been deleted.",
                    time = DateTime.UtcNow
                });

            return Ok(new { message = "Payment deleted successfully." });
        }
    }
}
