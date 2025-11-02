using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rently.Api.Models
{
    public class Rental
    {
        [Key]
        public int Id { get; set; }

        // Tenant reference (if you have multi-tenant system)
        [Required]
        public Guid TenantId { get; set; }

        // Link to Inventory Item
        [Required]
        public Guid ItemId { get; set; }

        [ForeignKey("ItemId")]
        public InventoryItem InventoryItem { get; set; }

        // Link to Customer
        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }  // ✅ Navigation property

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Optional: Track total payment or balance
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OutstandingAmount { get; set; }
    }
}
