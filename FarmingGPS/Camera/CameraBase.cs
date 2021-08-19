using System;
using System.Windows.Media.Imaging;

namespace FarmingGPS.Camera
{

    public abstract class CameraBase : ICamera, IDisposable
    {
        protected bool _connected;

        protected BitmapSource _bitmapImage;

        public virtual BitmapSource Bitmap
        {
            get { return _bitmapImage; }
        }

        public virtual bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public event EventHandler<CameraConnectedEventArgs> CameraConnectedChangedEvent;

        public event EventHandler<Exception> ExceptionEvent;

        public virtual void ChangeVideoFrameSize(int maxHeight)
        {
        }

        public virtual double SourceAspectRatio
        {
            get { return 1.0; }
        }

        protected virtual void OnCameraConnectedChanged(bool connected)
        {
            if(_connected != connected)
                if (CameraConnectedChangedEvent != null)
                    CameraConnectedChangedEvent.Invoke(this, new CameraConnectedEventArgs(connected));
            _connected = connected;
        }

        protected virtual void OnException(Exception e)
        {
            if (ExceptionEvent != null)
                ExceptionEvent.Invoke(this, e);
        }

        public virtual void Dispose()
        {
        }

    }
}
