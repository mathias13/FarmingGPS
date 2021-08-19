using System;
using System.Windows.Media.Imaging;

namespace FarmingGPS.Camera
{
    interface ICamera : IDisposable
    {
        event EventHandler<CameraConnectedEventArgs> CameraConnectedChangedEvent;

        event EventHandler<Exception> ExceptionEvent;

        void ChangeVideoFrameSize(int maxHeight);

        BitmapSource Bitmap
        {
            get;
        }

        double SourceAspectRatio
        {
            get;
        }

        bool Connected
        {
            get;
        }
    }
}
