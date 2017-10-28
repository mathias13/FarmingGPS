using System;
using System.Windows.Media.Imaging;

namespace FarmingGPS.Camera
{
    public abstract class CameraBase : ICamera, IDisposable
    {
        protected bool _connected;

        public virtual bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public event EventHandler<CameraConnectedEventArgs> CameraConnectedChangedEvent;

        public event EventHandler<CameraImageEventArgs> CameraImageEvent;

        protected virtual void OnCameraImage(BitmapImage bitmapImage)
        {
            if (CameraImageEvent != null)
                CameraImageEvent.Invoke(this, new CameraImageEventArgs(bitmapImage));
        }

        protected virtual void OnCameraConnectedChanged(bool connected)
        {
            if(_connected != connected)
                if (CameraConnectedChangedEvent != null)
                    CameraConnectedChangedEvent.Invoke(this, new CameraConnectedEventArgs(connected));
            _connected = connected;
        }

        public virtual void Dispose()
        {
        }
    }
}
