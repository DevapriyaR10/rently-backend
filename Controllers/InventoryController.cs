using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Rently.Api.Data;
using Rently.Api.Models;
using Rently.Api.Models.Dto;
using Rently.Api.Hubs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rently.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IHubContext<AlertHub> _hubContext;
        private readonly string _containerName = "inventory-images";

        public InventoryController(AppDbContext context, BlobServiceClient blobServiceClient, IHubContext<AlertHub> hubContext)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
            _hubContext = hubContext;
        }

        // ✅ GET: api/inventory?tenantId=xxx
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? tenantId)
        {
            var query = _context.InventoryItems.AsQueryable();
            if (tenantId.HasValue && tenantId != Guid.Empty)
                query = query.Where(i => i.TenantId == tenantId);

            var items = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.ImageUrl))
                    item.ImageUrl = GenerateSasUrl(item.ImageUrl);
            }

            return Ok(items);
        }

        // ✅ GET: api/inventory/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();

            if (!string.IsNullOrEmpty(item.ImageUrl))
                item.ImageUrl = GenerateSasUrl(item.ImageUrl);

            return Ok(item);
        }

        // ✅ POST: api/inventory
        [Authorize(Roles = "admin,manager")]
        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Create([FromForm] InventoryCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var item = new InventoryItem
            {
                ItemId = Guid.NewGuid(),
                TenantId = dto.TenantId,
                Name = dto.Name,
                Category = dto.Category,
                Condition = dto.Condition,
                Status = dto.Status,
                Price = dto.Price,
                CreatedAt = DateTime.UtcNow
            };

            // 🖼️ Upload image if available
            if (dto.ImageFile != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ImageFile.FileName)}";
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient(fileName);

                using (var stream = dto.ImageFile.OpenReadStream())
                    await blobClient.UploadAsync(stream, overwrite: true);

                item.ImageUrl = fileName;
            }

            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();

            // 🎯 Generate SAS URL
            if (!string.IsNullOrEmpty(item.ImageUrl))
                item.ImageUrl = GenerateSasUrl(item.ImageUrl);

            // 🚀 Real-time alert
            await SendTenantAlert(dto.TenantId, "success", "New Inventory Item Added", $"'{item.Name}' was added to your inventory.");

            return CreatedAtAction(nameof(Get), new { id = item.ItemId }, item);
        }

        // ✅ PUT: api/inventory/{id}
        [Authorize(Roles = "admin,manager")]
        [HttpPut("{id:guid}")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Update(Guid id, [FromForm] InventoryCreateDto dto)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null)
                return NotFound();

            bool updated = false;

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != item.Name)
            {
                item.Name = dto.Name;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Category) && dto.Category != item.Category)
            {
                item.Category = dto.Category;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Condition) && dto.Condition != item.Condition)
            {
                item.Condition = dto.Condition;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status) && dto.Status != item.Status)
            {
                item.Status = dto.Status;
                updated = true;
            }

            if (dto.Price > 0 && dto.Price != item.Price)
            {
                item.Price = dto.Price;
                updated = true;
            }

            if (dto.ImageFile != null)
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();

                if (!string.IsNullOrEmpty(item.ImageUrl))
                {
                    var oldBlob = containerClient.GetBlobClient(item.ImageUrl);
                    await oldBlob.DeleteIfExistsAsync();
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ImageFile.FileName)}";
                var blobClient = containerClient.GetBlobClient(fileName);
                using (var stream = dto.ImageFile.OpenReadStream())
                    await blobClient.UploadAsync(stream, overwrite: true);

                item.ImageUrl = fileName;
                updated = true;
            }

            if (!updated)
                return Ok(item); // Nothing changed, skip alert

            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(item.ImageUrl))
                item.ImageUrl = GenerateSasUrl(item.ImageUrl);

            // 🚀 Real-time alert
            await SendTenantAlert(item.TenantId, "info", "Inventory Updated", $"'{item.Name}' details were updated successfully.");

            return Ok(item);
        }

        // ✅ DELETE: api/inventory/{id}
        [Authorize(Roles = "admin,manager")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null)
                return NotFound();

            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(item.ImageUrl);
                await blobClient.DeleteIfExistsAsync();
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            // 🚀 Real-time alert
            await SendTenantAlert(item.TenantId, "warning", "Inventory Item Deleted", $"'{item.Name}' was removed from inventory.");

            return NoContent();
        }

        // 🔒 Generate SAS URL valid for 24 hours
        private string GenerateSasUrl(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
                return blobClient.Uri.ToString();

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        // 🧠 Helper: Send tenant alert (centralized)
        private async Task SendTenantAlert(Guid tenantId, string type, string title, string message)
        {
            if (tenantId == Guid.Empty) return;

            var alert = new
            {
                type,
                title,
                message,
                time = DateTime.UtcNow
            };

            await _hubContext.Clients.Group(tenantId.ToString()).SendAsync("ReceiveAlert", alert);
        }
    }
}
