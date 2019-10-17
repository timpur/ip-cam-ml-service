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

        IAsyncEnumerable<Bitmap> GetStream();
    }
}