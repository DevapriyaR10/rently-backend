using Microsoft.AspNetCore.Http;
using System;

namespace Rently.Api.Models.Dto
{
    public class InventoryCreateDto
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Condition { get; set; }
        public string Status { get; set; }
        public decimal Price { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
