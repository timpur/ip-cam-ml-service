using System;
using System.Collections.Generic;
using System.Drawing;

namespace IPCamMLService
{
    public interface IImageSource
    {
        string Name { get; }
        IAsyncEnumerable<Bitmap> GetStream();
    }
}