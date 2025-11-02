using System;
using System.ComponentModel.DataAnnotations;

namespace Rently.Api.Models
{
    public class Alert
    {
        [Key]
        public Guid AlertId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } // PendingPayment, UpcomingBooking, etc.

        [Required]
        [MaxLength(300)]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
