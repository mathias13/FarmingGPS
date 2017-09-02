using System;

namespace FarmingGPS.Camera
{
    public class CameraConnectedEventArgs : EventArgs
    {
        private bool _connected;

        public CameraConnectedEventArgs(bool connected)
        {
            _connected = connected;
        }

        public bool Connected
        {
            get { return _connected; }
        }
    }
}
