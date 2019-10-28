using System;
using System.Collections.Generic;
using System.Drawing;

namespace IPCamMLService.Models
{
    public class Frame : IFrame
    {
        public Bitmap Image { get; }

        public IList<IDetectedObject> DetectedObjects { get; } = new List<IDetectedObject>();

        public Frame(Bitmap image)
        {
            Image = image;
        }

    }

    public class DetectedObject : IDetectedObject
    {
        public Rectangle Box { get; }
        public IDictionary<string, IClassification> Classifications { get; } = new Dictionary<string, IClassification>();

        public DetectedObject(Rectangle box)
        {
            Box = box;
        }
    }

    public class Classification : IClassification
    {
        public string Label { get; }

        public float Probability { get; }

        public Classification(string label, float probability)
        {
            Label = label;
            Probability = probability;
        }
    }
}