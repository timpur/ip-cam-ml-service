using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.ML;
using IPCamMLService.Models;
using System.Drawing;
using Microsoft.ML.Transforms.Image;
using Microsoft.ML.Data;

namespace IPCamMLService.ML
{
    public class TinyYOLO2 : IObjectDetectionModel
    {
        public string Name { get; }

        private MLContext MLContext { get; }
        private PredictionEngine<Input, Output> ModelEngine { get; }

        private static string ModelFile = "./ML/tinyyolov2.zip";
        private static string ONNXModelFile = "./ML/tinyyolov2.onnx";

        public TinyYOLO2(string name)
        {
            Name = name;

            MLContext = new MLContext();
            ModelEngine = LoadModel();

            var file = File.Open("./ML/test.jpg", FileMode.Open);
            var img = new Bitmap(file);
            Predict(new Frame(img));
        }

        public IFrame Predict(IFrame frame)
        {
            var start = DateTime.Now;
            var input = new Input { Image = frame.Image };
            var output = ModelEngine.Predict(input);
            var end = DateTime.Now - start;
            Console.WriteLine($"ms: {end.Milliseconds}");
            ParseOutput(input, output, 0.3f);
            return frame;
        }

        private PredictionEngine<Input, Output> LoadModel()
        {
            if (!File.Exists(ModelFile)) CreateModel();

            DataViewSchema schema;
            var model = MLContext.Model.Load(ModelFile, out schema);
            return MLContext.Model.CreatePredictionEngine<Input, Output>(model);
        }

        private void CreateModel()
        {

            var dataView = MLContext.Data.LoadFromEnumerable(new List<Input>());
            var pipeline = MLContext.Transforms.ResizeImages(inputColumnName: nameof(Input.Image), imageHeight: Settings.ImageHeight, imageWidth: Settings.ImageWidth, resizing: ImageResizingEstimator.ResizingKind.Fill, outputColumnName: "image")
                .Append(MLContext.Transforms.ExtractPixels(inputColumnName: "image", outputColumnName: "image"))
                .Append(MLContext.Transforms.ApplyOnnxModel(inputColumnName: "image", modelFile: ONNXModelFile, outputColumnName: "grid"));
            var model = pipeline.Fit(dataView);

            MLContext.Model.Save(model, null, ModelFile);
        }

        private void ParseOutput(Input input, Output output, float threshold)
        {
            for (int row = 0; row < Settings.RowSize; row++)
            {
                for (int col = 0; col < Settings.ColSize; col++)
                {
                    for (int box = 0; box < Settings.Anchors.Length; box++)
                    {
                        var channel = box * (Settings.Labels.Length + Settings.FeaturesPerBox);
                        var features = GetBoxFeatures(output.Grid, row, col, box, channel);
                        if (features.Confidence < threshold) continue;
                        var probabilities = GetClassProbabilities(output.Grid, row, col, channel, features.Confidence)
                            .Where(x => x.propability >= threshold).ToArray();

                    }
                }
            }
        }

        private (float X, float Y, float Width, float Height, float Confidence) GetBoxFeatures(float[] grid, int row, int col, int box, int channel)
        {
            const int channelStride = Settings.RowSize * Settings.ColSize;
            int offsetConst = (col * Settings.ColSize) + row;
            int GetOffset() => (channel++ * channelStride) + offsetConst;

            const float cellHeight = Settings.ImageHeight / Settings.RowSize;
            const float cellWidth = Settings.ImageWidth / Settings.ColSize;

            var x = (row + Sigmoid(grid[GetOffset()]) * cellWidth);
            var y = (row + Sigmoid(grid[GetOffset()]) * cellHeight);
            var width = (MathF.Exp(grid[GetOffset()]) * cellWidth * Settings.Anchors[box].x);
            var height = (MathF.Exp(grid[GetOffset()]) * cellHeight * Settings.Anchors[box].y);
            var confidence = grid[GetOffset()];

            x -= x / 2;
            y -= y / 2;

            return (x, y, width, height, confidence);
        }

        private IEnumerable<(string label, float propability)> GetClassProbabilities(float[] grid, int row, int col, int channel, float confidence)
        {
            channel += Settings.FeaturesPerBox;
            const int channelStride = Settings.RowSize * Settings.ColSize;
            int offsetConst = (col * Settings.ColSize) + row;
            int GetOffset() => (channel++ * channelStride) + offsetConst;

            List<(string label, float propability)> result = new List<(string label, float propability)>();

            foreach (var label in Settings.Labels) result.Add((label, grid[GetOffset()]));
            return Softmax(result, confidence);
        }

        private static float Sigmoid(float value)
        {
            var k = MathF.Exp(value);
            return k / (1.0f + k);
        }

        private IEnumerable<(string label, float propability)> Softmax(IEnumerable<(string label, float propability)> probabilities, float confidence)
        {
            var max = probabilities.Max(x => x.propability);
            var exp = probabilities.Select(x => (x.label, propability: MathF.Exp(x.propability - max)));
            var sum = exp.Sum(x => x.propability);
            return exp.Select(x => (x.label, (x.propability / sum) * confidence));
        }

        private static class Settings
        {
            public const int RowSize = 13;
            public const int ColSize = 13;
            public const int FeaturesPerBox = 5;
            public static readonly (float x, float y)[] Anchors = { (1.08f, 1.19f), (3.42f, 4.41f), (6.63f, 11.38f), (9.42f, 5.11f), (16.62f, 10.52f) };
            public static readonly string[] Labels = {
                "aeroplane", "bicycle", "bird", "boat", "bottle",
                "bus", "car", "cat", "chair", "cow",
                "diningtable", "dog", "horse", "motorbike", "person",
                "pottedplant", "sheep", "sofa", "train", "tvmonitor"
            };
            public const int ImageHeight = 416;
            public const int ImageWidth = 416;
        }

        private class Input
        {
            [ImageType(Settings.ImageHeight, Settings.ImageWidth)]
            public Bitmap Image { get; set; }
        }

        private class Output
        {
            [ColumnName("grid")]
            public float[] Grid { get; set; }
        }
    }
}