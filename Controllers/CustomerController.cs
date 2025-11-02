using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rently.Api.Data;
using Rently.Api.Models;
using Rently.Api.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Rently.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureBlobService _blobService;

        public CustomerController(AppDbContext context, AzureBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: api/customer?tenantId=xxx
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid tenantId)
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .ToListAsync();

            return Ok(customers);
        }

        // GET: api/customer/{id}/kyc
[Authorize(Roles = "admin,manager,staff")]
[HttpGet("{id:guid}/kyc")]
public async Task<IActionResult> GetKycDocument(Guid id)
{
    var customer = await _context.Customers.FindAsync(id);
    if (customer == null)
        return NotFound(new { message = "Customer not found" });

    if (string.IsNullOrEmpty(customer.IdDocumentUrl))
        return BadRequest(new { message = "No KYC document found for this customer" });

    try
    {
        // Extract blob file name from stored URL
        var blobFileName = Path.GetFileName(new Uri(customer.IdDocumentUrl).LocalPath);

        // Generate temporary SAS URL (read-only, 10 mins)
        var sasUrl = _blobService.GenerateReadSasUrl("kyc-documents", blobFileName, expireMinutes: 10);

        return Ok(new
        {
            fileName = blobFileName,
            temporaryUrl = sasUrl,
            expiresInMinutes = 10
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Error generating SAS URL", error = ex.Message });
    }
}


        // POST: api/customer
        [Authorize(Roles = "admin,manager,staff")]
        [HttpPost]
        [RequestSizeLimit(10_000_000)] // 10MB
        public async Task<IActionResult> Create([FromForm] CustomerCreateDto dto)
        {
            string fileUrl = null;
            if (dto.IdDocument != null)
            {
                fileUrl = await _blobService.UploadAsync(
                    "kyc-documents",
                    dto.IdDocument.OpenReadStream(),
                    $"{Guid.NewGuid()}_{dto.IdDocument.FileName}"
                );
            }

            var customer = new Customer
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                TenantId = dto.TenantId,
                IdDocumentUrl = fileUrl
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(customer);
        }

        // PUT: api/customer/{id}
        [Authorize(Roles = "admin,manager")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Customer update)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            customer.FullName = update.FullName ?? customer.FullName;
            customer.Phone = update.Phone ?? customer.Phone;
            customer.Address = update.Address ?? customer.Address;
            customer.HasUnpaidDues = update.HasUnpaidDues;

            await _context.SaveChangesAsync();
            return Ok(customer);
        }

        // DELETE: api/customer/{id}
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // DTO for form data
    public class CustomerCreateDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public Guid TenantId { get; set; }
        public IFormFile IdDocument { get; set; }
    }
}
