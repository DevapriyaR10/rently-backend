namespace Rently.Api.DTOs
{
    public class CreateBookingDto
    {
        public Guid TenantId { get; set; }
        public Guid InventoryItemId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
