using System;

namespace FarmingGPS.Camera
{
    interface ICamera
    {
        event EventHandler<CameraImageEventArgs> CameraImageEvent;

        event EventHandler<CameraConnectedEventArgs> CameraConnectedChangedEvent;

        bool Connected
        {
            get;
        }
    }
}
