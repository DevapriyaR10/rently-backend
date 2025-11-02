using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rently.Api.Data;
using System;
using System.Threading.Tasks;

namespace Rently.Api.Controllers
{
    [Authorize] // Only allow logged-in users
    [ApiController]
    [Route("api/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TenantController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tenant/{tenantId}
        [HttpGet("{tenantId:guid}")]
        public async Task<IActionResult> GetTenant(Guid tenantId)
        {
            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);

            if (tenant == null)
                return NotFound(new { message = "Tenant not found" });

            return Ok(tenant);
        }
    }
}
