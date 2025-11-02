using System;
using System.ComponentModel.DataAnnotations;

namespace Rently.Api.Models
{
    public class Customer
    {
        [Key]
        public Guid CustomerId { get; set; } = Guid.NewGuid();

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        public string Address { get; set; }

        public string IdDocumentUrl { get; set; } // Azure Blob URL

        public bool HasUnpaidDues { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tenant relationship
        public Guid TenantId { get; set; }
    }
}
