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
    [ApiController]
    [Route("api/[controller]")]
    public class TenantRegistrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureBlobService _blobService;

        public TenantRegistrationController(AppDbContext context, AzureBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // ✅ POST: api/TenantRegistration/register
        [HttpPost("register")]
        [RequestSizeLimit(10_000_000)] // 10MB max
        public async Task<IActionResult> Register([FromForm] TenantCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid request data" });

            string logoUrl = null;
            string logoSasUrl = null;

            if (dto.Logo != null)
            {
                var fileName = $"{Guid.NewGuid()}_{dto.Logo.FileName}";

                // Upload to Azure Blob (container: tenant-logos)
                logoUrl = await _blobService.UploadAsync(
                    "tenant-logos",
                    dto.Logo.OpenReadStream(),
                    fileName
                );

                // Generate SAS URL valid for 1 hour
                logoSasUrl = _blobService.GenerateReadSasUrl("tenant-logos", fileName, 60);
            }

            var tenant = new Tenant
            {
                Name = dto.Name,
                Category = dto.Category,
                ThemeColor = dto.ThemeColor,
                LogoUrl = logoUrl,
                Email = dto.Email,           // New
                PhoneNumber = dto.PhoneNumber // New
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tenant registered successfully",
                tenant,
                logoSasUrl
            });
        }

        // ✅ GET: api/TenantRegistration
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenants = await _context.Tenants.AsNoTracking().ToListAsync();
            foreach (var tenant in tenants)
            {
                if (!string.IsNullOrEmpty(tenant.LogoUrl))
                {
                    try
                    {
                        var uri = new Uri(tenant.LogoUrl);
                        var fileName = Path.GetFileName(uri.LocalPath);
                        tenant.LogoUrl = _blobService.GenerateReadSasUrl("tenant-logos", fileName, 60);
                    }
                    catch
                    {
                        // ignore if invalid URL
                    }
                }
            }

            return Ok(tenants);
        }

        // ✅ GET: api/TenantRegistration/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
                return NotFound(new { message = "Tenant not found" });

            if (!string.IsNullOrEmpty(tenant.LogoUrl))
            {
                var uri = new Uri(tenant.LogoUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                tenant.LogoUrl = _blobService.GenerateReadSasUrl("tenant-logos", fileName, 60);
            }

            return Ok(tenant);
        }
    }

    // DTO for tenant registration
    public class TenantCreateDto
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string ThemeColor { get; set; }
        public IFormFile Logo { get; set; }

        // New fields
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
