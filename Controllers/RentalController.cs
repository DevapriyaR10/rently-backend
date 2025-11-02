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
    public class RentalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RentalController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Get all rentals for a tenant
        [HttpGet("{tenantId}")]
        public async Task<IActionResult> GetRentals(Guid tenantId)
        {
            var rentals = await _context.Rentals
                .Include(r => r.Customer)
                .Include(r => r.InventoryItem)
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();

            return Ok(rentals);
        }

        // 🔹 Get rental by Id
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetRental(int id)
        {
            var rental = await _context.Rentals
                .Include(r => r.Customer)
                .Include(r => r.InventoryItem)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null)
                return NotFound();

            return Ok(rental);
        }

        // 🔹 (Optional) Manually create rental (if needed)
        [HttpPost]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> CreateRental([FromBody] Rental rental)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            rental.StartDate = DateTime.UtcNow;
            rental.Status = "Active";
            rental.OutstandingAmount = rental.Price - (rental.AmountPaid ?? 0);

            _context.Rentals.Add(rental);
            await _context.SaveChangesAsync();

            return Ok(rental);
        }

        // 🔹 Mark rental as completed
        [HttpPut("{id}/complete")]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> CompleteRental(int id)
        {
            var rental = await _context.Rentals.FindAsync(id);
            if (rental == null)
                return NotFound();

            rental.Status = "Completed";
            rental.ReturnDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Rental marked as completed.", Rental = rental });
        }
    }
}
