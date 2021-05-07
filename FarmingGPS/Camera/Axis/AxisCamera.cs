using System;
using System.Net;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Rssdp;
using MjpegProcessor;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace FarmingGPS.Camera.Axis
{
    public class AxisCamera: CameraBase
    {
        protected readonly string AXIS_MJPEG_URI = "mjpg/1/video.mjpg";

        MjpegDecoder _decoder;

        SsdpDeviceLocator _locator;

        Timer _timeoutTimer;

        string _friendlyName = String.Empty;

        IPAddress _ipAddress = IPAddress.None;

        public AxisCamera()
        {
            _decoder = new MjpegDecoder();
            _decoder.FrameReady += _decoder_FrameReady;

            _timeoutTimer = new Timer(1.0);
            _timeoutTimer.Elapsed += _timeoutTimer_Elapsed;
            _timeoutTimer.Start();
        }

        public AxisCamera(string friendlyName) : this()
        {
            _friendlyName = friendlyName;
            _locator = new SsdpDeviceLocator();
        }

        public AxisCamera(IPAddress ipAddress) : this()
        {
            _ipAddress = ipAddress;
        }

        private void _timeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timeoutTimer.Stop();
            _timeoutTimer.Interval = 5000.0;
            OnCameraConnectedChanged(false);
            if (_friendlyName != String.Empty)
                SearchDevice();
            else if (_ipAddress != IPAddress.None)
                ConnectDevice();
            _timeoutTimer.Start();
        }

        private void _decoder_FrameReady(object sender, FrameReadyEventArgs e)
        {
            _timeoutTimer.Stop();
            OnCameraConnectedChanged(true);
            _timeoutTimer.Start();
        }

        private void ConnectDevice()
        {
            _decoder.StopStream();
            Uri decoderUri = new Uri(String.Format("http://{0}/{1}", _ipAddress.ToString(), AXIS_MJPEG_URI));
            _decoder.ParseStream(decoderUri);
        }

        private void SearchDevice()
        {
            Task<IEnumerable<DiscoveredSsdpDevice>> task = _locator.SearchAsync();
            task.Wait();
            foreach (DiscoveredSsdpDevice device in task.Result)
            {
                Task<SsdpDevice> taskGetDevice = device.GetDeviceInfo();
                taskGetDevice.Wait();
                if (taskGetDevice.Result.FriendlyName == _friendlyName)
                {
                    _decoder.StopStream();
                    _decoder.ParseStream(new Uri(taskGetDevice.Result.PresentationUrl + AXIS_MJPEG_URI));
                    break;
                }
            }
        }

        public override BitmapSource Bitmap
        {
            get { return _decoder.BitmapImage; }
        }

        public override void Dispose()
        {
            base.Dispose();
            _decoder.StopStream();
            if(_locator != null)
                _locator.Dispose();
            _timeoutTimer.Dispose();
        }
    }
}
