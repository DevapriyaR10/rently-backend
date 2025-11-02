using System;
using System.ComponentModel.DataAnnotations;

namespace Rently.Api.Models
{
    public class Tenant
    {
        [Key]
        public Guid TenantId { get; set; } = Guid.NewGuid(); // Unique ID

        [Required]
        public string Name { get; set; } // Business name

        public string Category { get; set; } // Camera, Car, Tools, etc.
        public string LogoUrl { get; set; } // For white-label branding
        public string ThemeColor { get; set; } // Optional for branding

        [EmailAddress]
        public string Email { get; set; } // Tenant email address

        [Phone]
        public string PhoneNumber { get; set; } // Tenant contact number
    }
}
