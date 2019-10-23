using System;
using System.Collections.Generic;
using System.Drawing;

namespace IPCamMLService.Models
{
    public interface IImageSource
    {
        string Name { get; }
        string Source { get; }
        int FPS { get; }

        IAsyncEnumerable<IFrame> GetStream();
    }

    public interface IFrame
    {
        Bitmap Image { get; }
        IList<IDetectedObject> DetectedObjects { get; }
    }

    public interface IDetectedObject
    {
        Rectangle Box { get; }
        IList<IClassification> Classifications { get; }
    }

    public interface IClassification
    {
        string Label { get; }
        float Probability { get; }
    }
}