using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

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

        public byte[] GetImageAsBytes()
        {
            var codec = ImageCodecInfo.GetImageDecoders().First(x => x.MimeType == "image/jpeg");
            var encoderParams = new EncoderParameters
            {
                Param = new EncoderParameter[]{
                    new EncoderParameter(Encoder.Quality, 50L)
                }
            };
            using var stream = new MemoryStream();
            Image.Save(stream, codec, encoderParams);
            return stream.GetBuffer();
        }

    }

    public class DetectedObject : IDetectedObject
    {
        public Rectangle Box { get; }
        public IList<IClassification> Classifications { get; } = new List<IClassification>();

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