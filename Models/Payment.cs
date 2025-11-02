using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Rently.Api.Models
{
    public class Payment
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public int RentalId { get; set; }

        [ForeignKey("RentalId")]
        [ValidateNever] // Prevents model validation errors when Rental not sent in request
        public Rental? Rental { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } // e.g., Cash, Card, UPI

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Completed";

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        public string? InvoiceUrl { get; set; } // Blob or local path
    }
}
