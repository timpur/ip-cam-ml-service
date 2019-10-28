using System;
using IPCamMLService.Models;

namespace IPCamMLService.ML
{
    public interface IObjectDetectionModel
    {
        string Name { get; }
        IFrame Predict(IFrame frame);
    }
}