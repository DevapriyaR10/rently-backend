using System;

namespace Rently.Api.Models
{
    public class AnalyticsLog
    {
        public int Id { get; set; }
        public Guid BookingId { get; set; }
        public Guid TenantId { get; set; }
        public string CustomerName { get; set; }
        public string Category { get; set; }
        public string Condition { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Revenue { get; set; }
        public DateTime LoggedAt { get; set; }
    }
}
