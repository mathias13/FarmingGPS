using FarmingGPS.Camera.FFmpeg;
using Newtonsoft.Json.Linq;
using RtspClientSharp;
using RtspClientSharp.RawFrames;
using RtspClientSharp.RawFrames.Video;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tmds.MDns;

namespace FarmingGPS.Camera.Garmin
{
    public class GarminVirb : CameraBase
    {
        private ServiceBrowser _serviceBrowser;

        private RtspClient _rtspClient;

        private CancellationTokenSource _cancellationToken;

        private IntPtr _decoderPointer = IntPtr.Zero;

        private IntPtr _scalerPointer = IntPtr.Zero;

        private object _syncObject = new object();

        private WriteableBitmap _writeableBitmap;

        private Int32Rect _dirtyRect;

        private byte[] _extraData;

        public GarminVirb()
        {
            ChangeVideoFrameSize(720);
            _serviceBrowser = new ServiceBrowser();
            _serviceBrowser.ServiceAdded += _serviceBrowser_ServiceAdded;
            _serviceBrowser.StartBrowse("_garmin-virb._tcp");
        }

        public override void ChangeVideoFrameSize(int maxHeight)
        {
            base.ChangeVideoFrameSize(maxHeight);

            var height = maxHeight;
            var width = maxHeight * SourceAspectRatio;

            _dirtyRect = new Int32Rect(0, 0, (int)width, height);

            lock (_syncObject)
            {
                if (_scalerPointer != IntPtr.Zero)
                {
                    FFmpegVideoPInvoke.RemoveVideoScaler(_scalerPointer);
                    _scalerPointer = IntPtr.Zero;
                }

                _writeableBitmap = new WriteableBitmap(
                    (int)width,
                    height,
                    96.0,
                    96.0,
                    PixelFormats.Pbgra32,
                    null);

                RenderOptions.SetBitmapScalingMode(_writeableBitmap, BitmapScalingMode.NearestNeighbor);

                _writeableBitmap.Lock();

                try
                {
                    UpdateBackgroundColor(_writeableBitmap.BackBuffer, _writeableBitmap.BackBufferStride);
                    _writeableBitmap.AddDirtyRect(_dirtyRect);
                }
                catch (Exception e1)
                {
                    OnException(e1);
                }
                finally
                {
                    _writeableBitmap.Unlock();
                }
            }
        }

        public override double SourceAspectRatio
        {
            get { return 1.7777777777777777777777777777778; }
        }

        public override BitmapSource Bitmap
        {
            get { return _writeableBitmap; }
        }

        public override void Dispose()
        {
            if(_rtspClient != null)
            {
                _rtspClient.FrameReceived -= _rtspClient_FrameReceived;
                _cancellationToken.Cancel();
            }    
            base.Dispose();
        }

        async void _serviceBrowser_ServiceAdded(object sender, ServiceAnnouncementEventArgs e)
        {
            if (_rtspClient == null)
            {
                var jobj = new JObject { { "command", "livePreview" }, { "streamType", "rtp" } };
                var adress = String.Format("http://{0}/virb", e.Announcement.Addresses[0].ToString());
                var response = Send(adress, jobj.ToString());

                if ((int)response["result"] == 1)
                {
                    var reconnect = true;
                    _cancellationToken = new CancellationTokenSource();
                    var connectionParameters = new ConnectionParameters(new Uri((string)response["url"]));
                    connectionParameters.RtpTransport = RtpTransportProtocol.UDP;
                    connectionParameters.CancelTimeout = TimeSpan.FromSeconds(1);
                    _rtspClient = new RtspClient(connectionParameters);
                    _rtspClient.FrameReceived += _rtspClient_FrameReceived;

                    while (reconnect)
                    {
                        reconnect = false;
                        _extraData = new byte[0];
                        int resultCode = FFmpegVideoPInvoke.CreateVideoDecoder((int)FFmpegVideoCodecId.H264, out _decoderPointer);

                        if (resultCode != 0)
                        {
                            OnException(new Exception($"An error occurred while creating video decoder, code: {resultCode}"));
                            return;
                        }

                        var connected = false;
                        try
                        {
                            await _rtspClient.ConnectAsync(_cancellationToken.Token);
                            connected = true;
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception e1)
                        {
                            OnException(e1);
                            reconnect = true;
                        }

                        if (connected)
                        {
                            try
                            {
                                OnCameraConnectedChanged(true);
                                await _rtspClient.ReceiveAsync(_cancellationToken.Token);
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            catch (Exception e1)
                            {
                                OnException(e1);
                                reconnect = true;
                            }
                        }

                        OnCameraConnectedChanged(false);
                        if (_decoderPointer != IntPtr.Zero)
                        {
                            _decoderPointer = IntPtr.Zero;
                            FFmpegVideoPInvoke.RemoveVideoDecoder(_decoderPointer);
                        }

                        if (_scalerPointer != IntPtr.Zero)
                        {
                            FFmpegVideoPInvoke.RemoveVideoScaler(_scalerPointer);
                            _scalerPointer = IntPtr.Zero;
                        }
                    }
                    _rtspClient.Dispose();
                    _rtspClient = null;
                }
            }
        }

        private unsafe void _rtspClient_FrameReceived(object sender, RawFrame e)
        {
            if (!(e is RawVideoFrame rawVideoFrame))
                return;

            fixed (byte* rawBufferPtr = &e.FrameSegment.Array[e.FrameSegment.Offset])
            {
                int resultCode;

                if (e is RawH264IFrame rawH264IFrame)
                {
                    if (rawH264IFrame.SpsPpsSegment.Array != null &&
                        !_extraData.SequenceEqual(rawH264IFrame.SpsPpsSegment))
                    {
                        if (_extraData.Length != rawH264IFrame.SpsPpsSegment.Count)
                            _extraData = new byte[rawH264IFrame.SpsPpsSegment.Count];

                        Buffer.BlockCopy(rawH264IFrame.SpsPpsSegment.Array, rawH264IFrame.SpsPpsSegment.Offset,
                            _extraData, 0, rawH264IFrame.SpsPpsSegment.Count);

                        fixed (byte* initDataPtr = &_extraData[0])
                        {
                            resultCode = FFmpegVideoPInvoke.SetVideoDecoderExtraData(_decoderPointer,
                                (IntPtr)initDataPtr, _extraData.Length);

                            if (resultCode != 0)
                            {
                                OnException(new Exception($"An error occurred while setting video extra data, code: {resultCode}"));
                                return;
                            }
                        }
                    }
                }

                resultCode = FFmpegVideoPInvoke.DecodeFrame(_decoderPointer, (IntPtr)rawBufferPtr,
                    e.FrameSegment.Count,
                    out int width, out int height, out FFmpegPixelFormat pixelFormat);

                if (resultCode != 0)
                {
                    OnException(new Exception($"An error occurred while decoding frame, code: {resultCode}"));
                    return;
                }

                lock (_syncObject)
                {
                    if (_scalerPointer == IntPtr.Zero && width > 0 && height > 0)
                    {
                        resultCode = FFmpegVideoPInvoke.CreateVideoScaler(0, 0, width, height,
                            pixelFormat, (int)_writeableBitmap.Width, (int)_writeableBitmap.Height, FFmpegPixelFormat.BGRA, FFmpegScalingQuality.FastBilinear, out _scalerPointer); ;

                        if (resultCode != 0)
                        {
                            OnException(new Exception($"An error occurred while creating video scaler, code: {resultCode}"));
                            return;
                        }
                    }

                    _writeableBitmap.Lock();

                    try
                    {
                        resultCode = FFmpegVideoPInvoke.ScaleDecodedVideoFrame(_decoderPointer, _scalerPointer, _writeableBitmap.BackBuffer, _writeableBitmap.BackBufferStride);

                        if (resultCode != 0)
                        {
                            OnException(new Exception($"An error occurred while scaling frame, code: {resultCode}"));
                            return;
                        }

                        _writeableBitmap.AddDirtyRect(_dirtyRect);
                    }
                    catch(Exception e1)
                    {
                        OnException(e1);
                    }
                    finally
                    {
                        _writeableBitmap.Unlock();
                    }
                }
            }
        }

        private JObject Send(string url, string json)
        {
            var content = string.Empty;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "Content-type: application/x-www-form-urlencoded";
                request.UserAgent = "curl / 7.38.0";
                request.Method = "POST";

                var buffer = Encoding.GetEncoding("UTF-8").GetBytes(json);
                var requestStream = request.GetRequestStream();
                requestStream.Write(buffer, 0, buffer.Length);
                requestStream.Close();


                var webRequest = request.GetResponse();
                var receiveStream = webRequest.GetResponseStream();
                var reader = new StreamReader(receiveStream, Encoding.UTF8);
                content = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
            if (string.IsNullOrEmpty(content))
                return null;

            return JObject.Parse(content);
        }
        
        private unsafe void UpdateBackgroundColor(IntPtr backBufferPtr, int backBufferStride)
        {
            byte* pixels = (byte*)backBufferPtr;
            int color = Colors.Black.A << 24 | Colors.Black.R << 16 | Colors.Black.G << 8 | Colors.Black.B;

            for (int i = 0; i < _writeableBitmap.Height; i++)
            {
                for (int j = 0; j < _writeableBitmap.Width; j++)
                    ((int*)pixels)[j] = color;

                pixels += backBufferStride;
            }
        }
    }
}
