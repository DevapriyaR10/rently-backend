using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;

namespace Rently.Api.ML
{
    public class RentalData
    {
        [LoadColumn(0)] public string Category { get; set; }
        [LoadColumn(1)] public string Condition { get; set; }
        [LoadColumn(2)] public float BasePrice { get; set; }
        [LoadColumn(3)] public float TimesRented { get; set; }
        [LoadColumn(4)] public string StartDate { get; set; }
        [LoadColumn(5)] public string EndDate { get; set; }

        // ✅ Label column
        [LoadColumn(6), ColumnName("Label")]
        public float RecommendedPrice { get; set; }
    }

    public class RentalPricePrediction
    {
        [ColumnName("Score")]
        public float PredictedPrice { get; set; }
    }

    public class RentlyPricingModel
    {
        private readonly string _modelPath = Path.Combine(Environment.CurrentDirectory, "MLModel.zip");
        private readonly string _dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "rentaldatabase.csv");
        private readonly MLContext _mlContext;
        private ITransformer _trainedModel;
        private PredictionEngine<RentalData, RentalPricePrediction> _predictionEngine;

        public RentlyPricingModel()
        {
            _mlContext = new MLContext();

            if (File.Exists(_modelPath))
            {
                Console.WriteLine("✅ Loading existing ML model...");
                _trainedModel = _mlContext.Model.Load(_modelPath, out _);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<RentalData, RentalPricePrediction>(_trainedModel);
            }
            else
            {
                Console.WriteLine("⚙️ Training new ML model...");
                TrainAndSaveModel();
            }
        }

        private void TrainAndSaveModel()
        {
            if (!File.Exists(_dataPath))
                throw new FileNotFoundException($"Training data not found at {_dataPath}");

            var dataView = _mlContext.Data.LoadFromTextFile<RentalData>(
                path: _dataPath,
                hasHeader: true,
                separatorChar: ',');

            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("CategoryEncoded", "Category")
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("ConditionEncoded", "Condition"))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    "CategoryEncoded", "ConditionEncoded", "BasePrice", "TimesRented"))
                .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", maximumNumberOfIterations: 100));

            _trainedModel = pipeline.Fit(dataView);

            Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
            _mlContext.Model.Save(_trainedModel, dataView.Schema, _modelPath);

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<RentalData, RentalPricePrediction>(_trainedModel);

            Console.WriteLine("✅ Model trained and saved successfully.");
        }

        public float PredictPrice(string category, string condition, float basePrice, float timesRented, string startDate, string endDate)
        {
            var input = new RentalData
            {
                Category = category,
                Condition = condition,
                BasePrice = basePrice,
                TimesRented = timesRented,
                StartDate = startDate,
                EndDate = endDate
            };

            var prediction = _predictionEngine.Predict(input);
            return prediction.PredictedPrice;
        }
    }
}
