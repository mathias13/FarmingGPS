using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FarmingGPS.Visualization;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using GpsUtilities;
using FarmingGPSLib.Positioning;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using ManagedUPnP;
using SwiftBinaryProtocol;
using SwiftBinaryProtocol.Eventarguments;
using SwiftBinaryProtocol.MessageStructs;
using GpsUtilities.Reciever;
using NTRIP;
using NTRIP.Settings;

namespace FarmingGPS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FarmingGPSLib.FieldItems.Field _field;
        
        private string _cameraIp = String.Empty;

        private MjpegProcessor.MjpegDecoder _decoder;

        private ClientService _ntripClient;

        private SBPReceiverSender _sbpReceiverSender;

        private IReceiver _receiver;

        private AutoEventedDiscoveryServices<Service> _mdsServices;

        private DateTime _trackingLineEvaluationTimeout = DateTime.MinValue;

        private TrackingLine _activeTrackingLine = null;

        private Coordinate _prevTrackCoordinate = new Coordinate(0.0, 0.0);

        private FieldTracker _fieldTracker = new FieldTracker();

        private FieldCreator _fieldCreator;

        private FarmingGPSLib.FarmingModes.GeneralHarrowingMode _farmingMode;

        private FarmingGPSLib.Equipment.IEquipment _equipment;

        private bool _ntripConnected = false;

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.Exit += Current_Exit;
            WindowState = WindowState.Maximized;

            System.Threading.Thread delayedActionsThread = new System.Threading.Thread(new System.Threading.ThreadStart(delayedActions));
            delayedActionsThread.Start();
            this.Loaded += MainWindow_Loaded;

            _mdsServices = new AutoEventedDiscoveryServices<Service>(null);
            _mdsServices.ResolveNetworkInterfaces = true;
            _mdsServices.StatusNotifyAction += mdsServices_StatusNotifyAction;
            _mdsServices.ReStartAsync();
            
        }

        void mdsServices_StatusNotifyAction(object sender, AutoEventedDiscoveryServices<Service>.StatusNotifyActionEventArgs e)
        {
            if (e.Data is Service && e.NotifyAction == AutoDiscoveryServices<Service>.NotifyAction.ServiceFound)
            {
                Service service = e.Data as Service;
                if (service.Device.FriendlyName == "Axis Case")
                {
                    _cameraIp = service.Device.PresentationURL;
                    _decoder = new MjpegProcessor.MjpegDecoder();
                    _decoder.FrameReady += FrameReady;
                    _decoder.ParseStream(new Uri(_cameraIp + "mjpg/1/video.mjpg"), "root", "vetinte13");
                }
            }
        }

        void FrameReady(object sender, MjpegProcessor.FrameReadyEventArgs e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                SetValue(CameraImageProperty, e.BitmapImage);
            }
            else
                Dispatcher.Invoke(DispatcherPriority.Render, new EventHandler<MjpegProcessor.FrameReadyEventArgs>(FrameReady), sender, e);
        }

        protected static readonly DependencyProperty CameraImageProperty = DependencyProperty.Register("CameraImage", typeof(BitmapImage), typeof(MainWindow));

        protected static readonly DependencyProperty CameraSizeProperty = DependencyProperty.Register("CameraSize", typeof(Style), typeof(MainWindow));
        
        void Current_Exit(object sender, ExitEventArgs e)
        {
            _ntripClient.Dispose();
            _receiver.Dispose();
            if(_sbpReceiverSender != null)
            _sbpReceiverSender.Dispose();
            _mdsServices.Dispose();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {   
            SetValue(CameraSizeProperty, (Style)this.FindResource("PiPvideo"));
        }
        
        void _ntripClient_StreamDataReceivedEvent(object sender, NTRIP.Eventarguments.StreamReceivedArgs e)
        {
            if(_sbpReceiverSender != null)
            _sbpReceiverSender.SendMessage(e.DataStream);
            if(!_ntripConnected)
            {
                _ntripConnected = true;
                ChangeNTRIPState();
        }
        }

        public delegate void FixUpdate(object sender, FixQuality fixQuality);

        void _receiver_FixQualityUpdate(object sender, FixQuality fixQuality)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                _fixMode.Text = fixQuality.ToString();
                if (fixQuality == FixQuality.FixedRealTimeKinematic)
                    _fixMode.Background = Brushes.Green;
                else
                    _fixMode.Background = Brushes.Red;
            }
            else
                Dispatcher.Invoke(new FixUpdate(_receiver_FixQualityUpdate), System.Windows.Threading.DispatcherPriority.Render, this, fixQuality);
        }

        void _receiver_PositionUpdate(object sender, Position actualPosition)
        {
            IReceiver receiver = sender as IReceiver;
            Coordinate actualCoordinate = _field.GetPositionInField(_equipment.GetCenter(actualPosition, receiver.CurrentBearing));
            Azimuth actualHeading = receiver.CurrentBearing;
            _visualization.UpdatePosition(actualCoordinate, actualHeading);
            
            //TODO Fix the distance needed before we draw a new track.
            if (!_fieldTracker.IsTracking)
            {
                Coordinate leftTip = _field.GetPositionInField(_equipment.GetLeftTip(actualPosition, actualHeading));
                Coordinate rightTip = _field.GetPositionInField(_equipment.GetRightTip(actualPosition, actualHeading));
                _fieldTracker.InitTrack(leftTip, rightTip);
                _prevTrackCoordinate = actualCoordinate;
            }
            else if (actualCoordinate.Distance(_prevTrackCoordinate) > 0.5)
            {
                Coordinate leftTip = _field.GetPositionInField(_equipment.GetLeftTip(actualPosition, actualHeading));
                Coordinate rightTip = _field.GetPositionInField(_equipment.GetRightTip(actualPosition, actualHeading));
                _fieldTracker.AddTrackPoint(leftTip, rightTip);
                _prevTrackCoordinate = actualCoordinate;
            } 

            if (DateTime.Now > _trackingLineEvaluationTimeout)
            {
                if (_farmingMode != null)
                {
                    TrackingLine newTrackingLine = _farmingMode.GetClosestLine(actualCoordinate);
                if (_activeTrackingLine == null)
                    _activeTrackingLine = newTrackingLine;
                    else if (!_activeTrackingLine.Equals(newTrackingLine))
                {
                    //TODO change depleted limit to a setting
                    if (_fieldTracker.GetTrackingLineCoverage(_activeTrackingLine) > 0.9)
                        _activeTrackingLine.Depleted = true;

                    _activeTrackingLine.Active = false;
                    _activeTrackingLine = newTrackingLine;
                }
                }
                
                //TODO Make this a setting instead 
                _trackingLineEvaluationTimeout = DateTime.Now.AddSeconds(5.0);
            }

            if (_activeTrackingLine != null)
            {
                _activeTrackingLine.Active = true;
                OrientationToLine orientationToLine = _activeTrackingLine.GetOrientationToLine(actualCoordinate, actualHeading);
                LightBar.Direction lightBarDirection = LightBar.Direction.Left;
                if (orientationToLine.SideOfLine == OrientationToLine.Side.Left)
                    lightBarDirection = LightBar.Direction.Right;

                _lightBar.SetDistance(Distance.FromMeters(orientationToLine.DistanceTo), lightBarDirection);
            }
        }

        void _receiver_BearingUpdate(object sender, Azimuth actualBearing)
        {
        }

        void _receiver_SpeedUpdate(object sender, Speed actualSpeed)
        {
            _speedBar.SetSpeed(actualSpeed);
        }

        void delayedActions()
        {
            System.Threading.Thread.Sleep(1000);
            List<Position> positions = new List<Position>();
            positions.Add(new Position(new Longitude(13.85490324303376), new Latitude(58.51282887426869)));
            positions.Add(new Position(new Longitude(13.85490181035013), new Latitude(58.51339680335879)));
            positions.Add(new Position(new Longitude(13.85428253752469), new Latitude(58.51339613650494)));
            positions.Add(new Position(new Longitude(13.85428227919927), new Latitude(58.51282947517689)));
            positions.Add(new Position(new Longitude(13.85490324303376), new Latitude(58.51282887426869)));

            _field = new Field(positions, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            FarmingGPSLib.Equipment.Harrow harrow = new FarmingGPSLib.Equipment.Harrow(Distance.FromMeters(5.0), Distance.FromMeters(0.0), new Azimuth(180), Distance.FromCentimeters(0));
            _equipment = harrow;

            _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, harrow, 1);
            _visualization.AddField(_field);
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
                _visualization.AddLine(line);

            //DotSpatial.Topology.Angle angle = new DotSpatial.Topology.Angle(0);
            //angle.DegreesPos = 99;
            //_farmingMode.CreateTrackingLines(field.GetPositionInField(new Position(new Longitude(13.855224), new Latitude(58.512617))), angle);
            _farmingMode.CreateTrackingLines(_farmingMode.TrackingLinesHeadLand[0]);

            foreach (TrackingLine line in _farmingMode.TrackingLines)
                _visualization.AddLine(line);

            _visualization.SetEquipmentWidth(harrow.Width);

            _speedBar.Unit = SpeedUnit.KilometersPerHour;
            _speedBar.SetSpeed(Speed.FromKilometersPerHour(2.4));
            _receiver_FixQualityUpdate(this, FixQuality.FixedRealTimeKinematic);
            //_visualization.UpdatePosition(_field.GetPositionInField(new Position(new Longitude(13.85490324303376), new Latitude(58.51282887426869))), Azimuth.North);
            //_visualization.UpdatePosition(_fieldCreator.GetField().GetPositionInField(new Position(new Longitude(13.8547112149059), new Latitude(58.5126434260099))), new Azimuth(90));
            _visualization.AddFieldTracker(_fieldTracker);

            //_sbpReceiverSender = new SBPReceiverSender(System.Net.IPAddress.Parse("192.168.0.222"), 55555);
            _sbpReceiverSender = new SBPReceiverSender("COM4", 115200, false);
            _receiver = new Piksi(_sbpReceiverSender, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(1000));
            _receiver.MinimumSpeedLockHeading = Speed.FromKilometersPerHour(1.0);
            //_receiver = new KeyboardSimulator(this, new Position3D(Distance.FromMeters(0.0), new Longitude(13.8547112149059), new Latitude(58.5126434260099)), false);
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
            _speedBar.Unit = SpeedUnit.KilometersPerHour;

            NTRIP.Settings.ClientSettings clientSettings = new NTRIP.Settings.ClientSettings();
            clientSettings.IPorHost = "nolgarden.net";
            clientSettings.PortNumber = 5000;
            clientSettings.NTRIPMountPoint = "NolgardenSBP";
            clientSettings.NTRIPUser = new NTRIP.Settings.NTRIPUser("Mathias", "vetinte");
            _ntripClient = new NTRIP.ClientService(clientSettings);
            _ntripClient.StreamDataReceivedEvent += _ntripClient_StreamDataReceivedEvent;
            _ntripClient.ConnectionExceptionEvent += _ntripClient_ConnectionExceptionEvent;
            _ntripClient.Connect();
        }

        private void _ntripClient_ConnectionExceptionEvent(object sender, NTRIP.Eventarguments.ConnectionExceptionArgs e)
        {
            if(_ntripConnected)
            {
                _ntripConnected = false;
                ChangeNTRIPState();
            }
            _ntripClient.Connect();
        }

        private void ChangeNTRIPState()
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                if (_ntripConnected)
                    _NTRIPState.Background = Brushes.Green;
                else
                    _NTRIPState.Background = Brushes.Red;
            }
            else
                Dispatcher.Invoke(new Action(ChangeNTRIPState), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void _fieldCreator_FieldCreated(object sender, FieldCreatedEventArgs e)
        {
            _field = e.Field;
            _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, _equipment, 1);
            DotSpatial.Topology.Angle angle = new DotSpatial.Topology.Angle(0);
            angle.DegreesPos = 99;
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
            {
                _visualization.AddLine(line);
                if (_fieldTracker.GetTrackingLineCoverage(line) > 0.9)
                    line.Depleted = true;
            }
            _farmingMode.CreateTrackingLines(_farmingMode.TrackingLinesHeadLand[3]);
            //_farmingMode.CreateTrackingLines(_field.GetPositionInField(new Position(new Longitude(13.855224), new Latitude(58.512617))), angle);
            foreach (TrackingLine line in _farmingMode.TrackingLines)
            {
                _visualization.AddLine(line);
                if (_fieldTracker.GetTrackingLineCoverage(line) > 0.9)
                    line.Depleted = true;
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Style fullvideo = (Style)this.FindResource("Fullvideo");
            Style pipvideo = (Style)this.FindResource("PiPvideo");
            if (GetValue(CameraSizeProperty).Equals(pipvideo))
                SetValue(CameraSizeProperty, fullvideo);
            else if (GetValue(CameraSizeProperty).Equals(fullvideo))
                SetValue(CameraSizeProperty, pipvideo);
        }

        #region Button Events

        private void BTN_ZOOM_IN_Click(object sender, RoutedEventArgs e)
        {
            _visualization.ZoomIn();
        }

        private void BTN_ZOOM_OUT_Click(object sender, RoutedEventArgs e)
        {
            _visualization.ZoomOut();
        }

        private void BTN_VIEW_CHANGE_Click(object sender, RoutedEventArgs e)
        {
            _visualization.ChangeView();
        }

        private void BTN_START_FIELD_Click(object sender, RoutedEventArgs e)
        {
            //if (_fieldCreator == null)
            //{
            //    _fieldCreator = new FieldCreator(DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N, FieldCreator.Orientation.Lefthand, _receiver, _equipment);
            //    _fieldCreator.FieldCreated += _fieldCreator_FieldCreated;
            //    _field = _fieldCreator.GetField();
            //    _visualization.AddFieldCreator(_fieldCreator);
            //}
        }

        private void BTN_SETTINGS_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsGrid.Visibility == System.Windows.Visibility.Hidden)
                _settingsGrid.Visibility = System.Windows.Visibility.Visible;
            else
                _settingsGrid.Visibility = System.Windows.Visibility.Hidden;
        }

        #endregion

    }
}
