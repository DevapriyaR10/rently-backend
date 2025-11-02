using System;
using System.ComponentModel.DataAnnotations;

namespace Rently.Api.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; } // Link to Tenant
        public string Email { get; set; }
        public string PasswordHash { get; set; } // Never store plain password
        public string Role { get; set; } // Admin, Manager, Staff

        public Tenant Tenant { get; set; } // Navigation property
    }
}
