using System;
using System.Windows.Media.Imaging;

namespace FarmingGPS.Camera
{
    public class CameraImageEventArgs : EventArgs
    {
        BitmapImage _image;

        public CameraImageEventArgs(BitmapImage bitmapImage)
        {
            _image = bitmapImage;
        }
        
        public BitmapImage BitmapImage
        {
            get { return _image; }
        }

    }
}
