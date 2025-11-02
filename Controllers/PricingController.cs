using Microsoft.AspNetCore.Mvc;
using Rently.Api.Services;

namespace Rently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PricingController : ControllerBase
    {
        private readonly PricingService _pricingService;

        public PricingController()
        {
            _pricingService = new PricingService();
        }

        // ✅ Local DTO (no separate file)
        public class PricingRequest
        {
            public string Category { get; set; }
            public string Condition { get; set; }
            public float BasePrice { get; set; }
            public float TimesRented { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
        }

        [HttpPost("recommend")]
        public IActionResult RecommendPrice([FromBody] PricingRequest request)
        {
            if (request == null)
                return BadRequest("Invalid input data.");

            float recommended = _pricingService.GetRecommendedPrice(
                request.Category,
                request.Condition,
                request.BasePrice,
                request.TimesRented,
                request.StartDate,
                request.EndDate
            );

            return Ok(new { recommendedPrice = recommended });
        }
    }
}
