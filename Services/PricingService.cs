using Rently.Api.ML;

namespace Rently.Api.Services
{
    public class PricingService
    {
        private readonly RentlyPricingModel _pricingModel;

        public PricingService()
        {
            _pricingModel = new RentlyPricingModel();
        }

        public float GetRecommendedPrice(string category, string condition, float basePrice, float timesRented, string startDate, string endDate)
        {
            return _pricingModel.PredictPrice(category, condition, basePrice, timesRented, startDate, endDate);
        }
    }
}
