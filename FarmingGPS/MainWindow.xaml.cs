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
        private FarmingGPSLib.FieldItems.Field field;
        
        private string _cameraIp = String.Empty;

        private MjpegProcessor.MjpegDecoder _decoder;

        private ClientService _ntripClient;

        private SBPReceiverSender _sbpReceiverSender;

        private IReceiver _receiver;

        private AutoEventedDiscoveryServices<Service> _mdsServices;

        private Coordinate _actualCoordinate = new Coordinate(0.0, 0.0);

        private Azimuth _actAngle = Azimuth.North;

        private DateTime _trackingLineEvaluationTimeout = DateTime.MinValue;

        private TrackingLine _activeTrackingLine = null;

        private Coordinate _prevTrackCoordinate = new Coordinate(0.0, 0.0);

        private FieldTracker _fieldTracker = new FieldTracker();

        private bool _fieldTrackerActive = false;

        private bool _fieldTrackReinit = false;

        private FarmingGPSLib.FarmingModes.GeneralHarrowingMode _farmingMode;

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
            _sbpReceiverSender.Dispose();
            _mdsServices.Dispose();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PLAY"));
            SetValue(CameraSizeProperty, (Style)this.FindResource("PiPvideo"));
        }
        
        void _ntripClient_StreamDataReceivedEvent(object sender, NTRIP.Eventarguments.StreamReceivedArgs e)
        {
            _sbpReceiverSender.SendMessage(e.DataStream);
        }

        public delegate void FixUpdate(object sender, FixQuality fixQuality);

        void _receiver_FixQualityUpdate(object sender, FixQuality fixQuality)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                _fixMode.Text = fixQuality.ToString();
            }
            else
                Dispatcher.Invoke(new FixUpdate(_receiver_FixQualityUpdate), System.Windows.Threading.DispatcherPriority.Render, this, fixQuality);
        }

        void _receiver_PositionUpdate(object sender, Position actualPosition)
        {
            _actualCoordinate = field.GetPositionInField(actualPosition);
            _visualization.UpdatePosition(_actualCoordinate, _actAngle);
            
            //TODO Fix the distance needed before we draw a new track.
            if (!_fieldTracker.IsTracking)
            {
                FarmingGPSLib.FarmingModes.EquipmentTips equipment = _farmingMode.GetEquipmentTips(_actualCoordinate, _actAngle);
                _fieldTracker.InitTrack(equipment.LeftTip, equipment.RightTip);
                _prevTrackCoordinate = _actualCoordinate;
            }
            else if (_actualCoordinate.Distance(_prevTrackCoordinate) > 0.5)
            {
                FarmingGPSLib.FarmingModes.EquipmentTips equipment = _farmingMode.GetEquipmentTips(_actualCoordinate, _actAngle);
                _fieldTracker.AddTrackPoint(equipment.LeftTip, equipment.RightTip);
                _prevTrackCoordinate = _actualCoordinate;
            } 

            if(DateTime.Now > _trackingLineEvaluationTimeout)
            {
                TrackingLine newTrackingLine = _farmingMode.GetClosestLine(_actualCoordinate);
                if (_activeTrackingLine == null)
                    _activeTrackingLine = newTrackingLine;
                else if(!_activeTrackingLine.Equals(newTrackingLine))
                {
                    //TODO change depleted limit to a setting
                    if (_fieldTracker.GetTrackingLineCoverage(_activeTrackingLine) > 0.9)
                        _activeTrackingLine.Depleted = true;

                    _activeTrackingLine.Active = false;
                    _activeTrackingLine = newTrackingLine;
                }
                
                //TODO Make this a setting instead 
                _trackingLineEvaluationTimeout = DateTime.Now.AddSeconds(5.0);
            }

            if(_activeTrackingLine != null)
            {
                _activeTrackingLine.Active = true;
                OrientationToLine orientationToLine = _activeTrackingLine.GetOrientationToLine(_actualCoordinate, _actAngle);
                LightBar.Direction lightBarDirection = LightBar.Direction.Left;
                if (orientationToLine.SideOfLine == OrientationToLine.Side.Left)
                    lightBarDirection = LightBar.Direction.Right;

                _lightBar.SetDistance(Distance.FromMeters(orientationToLine.DistanceTo), lightBarDirection);
            }
        }

        void _receiver_BearingUpdate(object sender, Azimuth actualBearing)
        {
            _actAngle = actualBearing;
            _visualization.UpdatePosition(_actualCoordinate, _actAngle);
        }

        void _receiver_SpeedUpdate(object sender, Speed actualSpeed)
        {
            _speedBar.SetSpeed(actualSpeed);
        }

        void delayedActions()
        {
            _sbpReceiverSender = new SBPReceiverSender("COM6", 1000000);
            _receiver = new KeyboardSimulator(this, new CartesianPoint(Distance.FromMeters(3242347.98639), Distance.FromMeters(799848.358036), Distance.FromMeters(5415851.61154)).ToPosition3D());
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
            _speedBar.Unit = SpeedUnit.KilometersPerHour;

            NTRIP.Settings.ClientSettings clientSettings = new NTRIP.Settings.ClientSettings();
            clientSettings.IPorHost = "nolgarden.net";
            clientSettings.PortNumber = 5000;
            clientSettings.NTRIPMountPoint = "ExampleName";
            clientSettings.NTRIPUser = new NTRIP.Settings.NTRIPUser("ExAmPlEuSeR", "ExAmPlEpAsSwOrD");
            _ntripClient = new NTRIP.ClientService(clientSettings);
            _ntripClient.StreamDataReceivedEvent += _ntripClient_StreamDataReceivedEvent;
            _ntripClient.Connect();

            System.Threading.Thread.Sleep(1000);
            List<Position> positions = new List<Position>();
            positions.Add(new Position(new Longitude(13.855224), new Latitude(58.512617)));
            positions.Add(new Position(new Longitude(13.855385), new Latitude(58.512526)));
            positions.Add(new Position(new Longitude(13.854799), new Latitude(58.511009)));
            positions.Add(new Position(new Longitude(13.855010), new Latitude(58.510682)));
            positions.Add(new Position(new Longitude(13.862226), new Latitude(58.509660)));
            positions.Add(new Position(new Longitude(13.864235), new Latitude(58.513386)));
            positions.Add(new Position(new Longitude(13.859803), new Latitude(58.514010)));
            positions.Add(new Position(new Longitude(13.859956), new Latitude(58.514921)));
            positions.Add(new Position(new Longitude(13.855927), new Latitude(58.515144)));
            positions.Add(new Position(new Longitude(13.855224), new Latitude(58.512617)));

            field = new Field(positions, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            FarmingGPSLib.Equipment.Harrow harrow = new FarmingGPSLib.Equipment.Harrow(Distance.FromMeters(6), Distance.FromMeters(1.5), new DotSpatial.Positioning.Angle(180), Distance.FromCentimeters(20));
            _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(field, harrow, 1);

            _visualization.AddField(field);
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
                _visualization.AddLine(line);

            DotSpatial.Topology.Angle angle = new DotSpatial.Topology.Angle(0);
            angle.DegreesPos = 99;
            _farmingMode.CreateTrackingLines(field.GetPositionInField(new Position(new Longitude(13.855224), new Latitude(58.512617))), angle);

            foreach (TrackingLine line in _farmingMode.TrackingLines)
                _visualization.AddLine(line);

            _visualization.SetEquipmentWidth(harrow.Width);

            _speedBar.Unit = SpeedUnit.KilometersPerHour;
            _speedBar.SetSpeed(Speed.FromKilometersPerHour(2.4));
            _receiver_FixQualityUpdate(this, FixQuality.FixedRealTimeKinematic);
            _visualization.UpdatePosition(field.GetPositionInField(new Position(new Longitude(13.8547112149059), new Latitude(58.5126434260099))), new Azimuth(90));
            _visualization.AddFieldTracker(_fieldTracker);
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

        #region DependencyProperties

        protected static readonly DependencyProperty FieldTrackerButtonStyleProperty = DependencyProperty.Register("FieldTrackerButtonStyle", typeof(Style), typeof(MainWindow));

        #endregion

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

        private void BTN_PLAY_TRACKER_Click(object sender, RoutedEventArgs e)
        {
            _fieldTrackerActive = !_fieldTrackerActive;
            if (_fieldTrackerActive)
            {
                _fieldTrackReinit = true;
                SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PAUSE"));
            }
            else
                SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PLAY"));
        }

        #endregion
    }
}
