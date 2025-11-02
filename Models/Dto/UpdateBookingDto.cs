namespace Rently.Api.DTOs
{
    public class UpdateBookingDto
    {
        public string? CustomerName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
    }
}
