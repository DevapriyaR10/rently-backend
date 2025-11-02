using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rently.Api.Models
{
    public class Booking
    {
        [Key]  // ✅ Explicitly mark as primary key
        public Guid BookingId { get; set; }

        [Required]
        public Guid CustomerId { get; set; }  // ✅ Foreign key to Customer

        [Required]
        public Guid TenantId { get; set; }    // ✅ Foreign key to Tenant

        [Required]
        public Guid InventoryItemId { get; set; }  // ✅ Foreign key to InventoryItem

        [Required, MaxLength(100)]
        public string CustomerName { get; set; }

        [Required, EmailAddress]
        public string CustomerEmail { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // ✅ Navigation Properties
        [ForeignKey(nameof(InventoryItemId))]
        public InventoryItem InventoryItem { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; }

        [ForeignKey(nameof(TenantId))]
        public Tenant Tenant { get; set; }
    }
}
