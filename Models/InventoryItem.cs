using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rently.Api.Models
{
    public class InventoryItem
    {
        [Key]
        public Guid ItemId { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [MaxLength(50)]
        public string Condition { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Available";

        // ← Add this property
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } = 0m;

        public string ImageUrl { get; set; }

        public int TimesRented { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
