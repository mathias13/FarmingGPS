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
        //private NmeaInterpreter _interpreter = new NmeaInterpreter();

        private int _lastSecond = 0;

        //private NmeaInterpreter _interpreter1 = new NmeaInterpreter();

        private int _lastSecond1 = 0;

        //private GPSReceiverManager _receiverManager;

        private bool stopGpsFinder = false;

        //private System.Threading.Thread gpsThread;

        private Position _southWest;

        private Segment line;

        private Segment line1;

        private Segment line2;

        private Segment line3;

        private FarmingGPSLib.FieldItems.Field field;

        private KalmanFilter _kalmanFilter;

        private Position _lastPosition;

        private string _cameraIp = String.Empty;

        private MjpegProcessor.MjpegDecoder _decoder;

        private ClientService _ntripClient;

        private SBPReceiverSender _sbpReceiverSender;

        private IReceiver _receiver;

        private AutoEventedDiscoveryServices<Service> _mdsServices;

        private Coordinate _actualCoordinate = new Coordinate(0.0, 0.0);

        private Coordinate _prevTrackCoordinate = new Coordinate(0.0, 0.0);

        private FieldTracker _fieldTracker = new FieldTracker();

        private FarmingGPSLib.FarmingModes.GeneralHarrowingMode _farmingMode;

        private Azimuth _actAngle = Azimuth.North;

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.Exit += Current_Exit;
            WindowState = WindowState.Maximized;

            //_interpreter.PositionChanged += _interpreter_PositionChanged;
            //_interpreter.PositionReceived += _interpreter_PositionReceived;
            //AverageFilter filter = new AverageFilter(3000);
            //CdfFilter filter = new CdfFilter();
            //_interpreter.IsFilterEnabled = true;

            //_interpreter1.PositionChanged += _interpreter1_PositionChanged;
            //_interpreter1.IsFilterEnabled = false;

            //_receiverManager = new GPSReceiverManager(TimeSpan.FromSeconds(0.5));
            //_receiverManager.PositionUpdateEvent += _receiverManager_PositionUpdateEvent;

            //gpsThread = new System.Threading.Thread(new System.Threading.ThreadStart(gpsFinder));
            //gpsThread.Start();
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
            //_receiverManager.Dispose();
            stopGpsFinder = true;
            //gpsThread.Join();
            //_interpreter.Stop();
            //if (_interpreter.Device != null)
            //{
            //    _interpreter.Device.Close();
            //    _interpreter.Device.Dispose();
            //}

            //_interpreter1.Stop();
            //if (_interpreter1.Device != null)
            //{
            //    _interpreter1.Device.Close();
            //    _interpreter1.Device.Dispose();
            //}
            _ntripClient.Dispose();
            _receiver.Dispose();
            _sbpReceiverSender.Dispose();
            _mdsServices.Dispose();
        }

        //void gpsFinder()
        //{
        //    bool newSearch = false;
        //    Devices.DeviceDetectionTimeout = TimeSpan.FromMinutes(1);
        //    while (!stopGpsFinder)
        //    {
        //        if (!_interpreter.IsRunning || !_interpreter1.IsRunning)
        //        {
        //            Devices.AllowSerialConnections = true;
        //            Devices.AllowBluetoothConnections = true;
        //            if (!Devices.IsDetectionInProgress && !newSearch)
        //            {
        //                newSearch = true;
        //                Devices.BeginDetection();
        //            }
        //            else if(!Devices.IsDetectionInProgress && newSearch)
        //            {
        //                newSearch = false;
        //                foreach (Device device in Devices.GpsDevices)
        //                {
        //                    if (!_interpreter.IsRunning && !device.Equals(_interpreter1.Device))
        //                        _interpreter.Start(device);
        //                    if (!_interpreter1.IsRunning && !device.Equals(_interpreter.Device))
        //                        _interpreter1.Start(device);
        //                }
        //            }
        //            //System.Threading.Thread.Sleep(TimeSpan.FromMinutes(0.4));
        //            //if (Devices.GpsDevices.Count > 0)
        //            //    _interpreter.Start();
        //        }                               
        //        //Devices.BeginDetection();
        //        //Devices.WaitForDetection(TimeSpan.FromMinutes(1.0));
        //        //if (Devices.GpsDevices.Count > 1)
        //        //{
        //        //    Distance[] distances1 = new Distance[1] { Distance.FromCentimeters(100) };
        //        //    Azimuth[] bearings1 = new Azimuth[1] { new Azimuth(90) };
        //        //    Distance[] distances2 = new Distance[1] { Distance.FromCentimeters(100) };
        //        //    Azimuth[] bearings2 = new Azimuth[1] { new Azimuth(-90) };
        //        //    Reposition repos1 = new Reposition(distances1, bearings1);
        //        //    Reposition repos2 = new Reposition(distances2, bearings2);
        //        //    KeyValuePair<Device, Reposition>? device1 = null;
        //        //    KeyValuePair<Device, Reposition>? device2 = null;

        //        //    if (Devices.GpsDevices[0].Name == "BT-GPS-39042C")
        //        //        device1 = new KeyValuePair<Device, Reposition>(Devices.GpsDevices[0], repos1);
        //        //    else if (Devices.GpsDevices[1].Name == "BT-GPS-39042C")
        //        //        device1 = new KeyValuePair<Device, Reposition>(Devices.GpsDevices[1], repos1);


        //        //    if (Devices.GpsDevices[0].Name == "BT-GPS-38FCEF")
        //        //        device2 = new KeyValuePair<Device, Reposition>(Devices.GpsDevices[0], repos2);
        //        //    else if (Devices.GpsDevices[1].Name == "BT-GPS-38FCEF")
        //        //        device2 = new KeyValuePair<Device, Reposition>(Devices.GpsDevices[1], repos2);

        //        //    _receiverManager.Start(device1, device2);

        //        //    return;
        //        //}
        //        System.Threading.Thread.Sleep(100);
        //    }

        //}

        //void _interpreter_PositionReceived(object sender, PositionEventArgs e)
        //{
        //    if (e.Position.IsInvalid)
        //        return;
        //    if (_kalmanFilter == null)
        //        _kalmanFilter = new KalmanFilter(new Position3D(e.Position), Distance.FromMeters(6.0), _interpreter.HorizontalDilutionOfPrecision, _interpreter.VerticalDilutionOfPrecision, Ellipsoid.Default);
        //    if (!_kalmanFilter.IsInitialized)
        //        _kalmanFilter.Initialize(e.Position);


        //    _visualization.AddPoint(field.GetPositionInField(e.Position), Colors.Red);
        //}

        //void _interpreter_PositionChanged(object sender, PositionEventArgs e)
        //{
        //    if (_lastSecond == DateTime.Now.Second)
        //        return;
        //    _visualization.AddPoint(field.GetPositionInField(e.Position), Colors.Red);
        //    return;
        //    //if (filter.CurrentErrorEstimate.ToMeters().Value < 0.5)
        //        //_visualization.AddPoint(field.GetPositionInField(e.Position), Colors.Blue);
        //    //else
        //        //_visualization.AddPoint(field.GetPositionInField(e.Position), Colors.Red);

        //    //return;

        //    //else
        //        //_visualization.AddPoint(field.GetPositionInField(e.Position), Colors.Red);

        //    return;

        //    Distance distanceBack = Distance.FromCentimeters(275);
        //    Distance distanceSideways = Distance.FromCentimeters(20);
        //    //Position corrected = e.Position.TranslateTo((_interpreter.Filter as CdfFilter).FilteredHeading.Mirror(), distanceBack).TranslateTo((_interpreter.Filter as CdfFilter).FilteredHeading.Add(90.0), distanceSideways);
        //    //Position corrected = e.Position;
        //    Position corrected = e.Position.TranslateTo(_interpreter.Bearing.Mirror(), distanceBack).TranslateTo(_interpreter.Bearing.Add(90.0), distanceSideways);

        //    //int lineId = 0;
        //    //Segment closestLine = line;
        //    //double distanceFrom = line.DistanceTo(corrected).Value;
        //    //if (line1.DistanceTo(corrected).Value < distanceFrom)
        //    //{
        //    //    distanceFrom = line1.DistanceTo(corrected).Value;
        //    //    closestLine = line1;
        //    //    lineId = 1;
        //    //}
        //    //if (line2.DistanceTo(corrected).Value < distanceFrom)
        //    //{
        //    //    distanceFrom = line2.DistanceTo(corrected).Value;
        //    //    closestLine = line2;
        //    //    lineId = 2;
        //    //}
        //    //if (line3.DistanceTo(corrected).Value < distanceFrom)
        //    //{
        //    //    distanceFrom = line3.DistanceTo(corrected).Value;
        //    //    closestLine = line3;
        //    //    lineId = 3;
        //    //}


        //    //bool leftFalseRightTrue = false;
        //    //if (_interpreter.Bearing.IsBetween(closestLine.Bearing.Subtract(89).Normalize(), closestLine.Bearing.Add(89).Normalize()))
        //    //{
        //    //    if (e.Position.BearingTo(closestLine.End).Subtract(closestLine.Bearing).Normalize().IsBetween(Azimuth.North, Azimuth.East))
        //    //        leftFalseRightTrue = true;
        //    //}
        //    //else
        //    //    if (e.Position.BearingTo(closestLine.Start).Subtract(closestLine.Bearing.Subtract(Azimuth.South).Normalize()).Normalize().IsBetween(Azimuth.North, Azimuth.East))
        //    //        leftFalseRightTrue = true;

        //    //if (leftFalseRightTrue)
        //    //    _lightBar.SetDistance(closestLine.DistanceTo(e.Position), LightBar.Direction.Right);
        //    //else
        //    //    _lightBar.SetDistance(closestLine.DistanceTo(e.Position), LightBar.Direction.Left);

        //    //_visualization.UpdatePosition(field.GetPositionInField(corrected), (_interpreter.Filter as CdfFilter).FilteredHeading.DecimalDegrees);
        //    _visualization.UpdatePosition(field.GetPositionInField(corrected), _interpreter.Bearing.DecimalDegrees);

        //    //_visualization.UpdatePosition(GetPoint(corrected), (_interpreter.Filter as CdfFilter).FilteredHeading.DecimalDegrees);
        //    //_visualization.UpdatePosition(GetPoint(corrected), _interpreter.Bearing.DecimalDegrees);
        //    //_visualization.SetTrackingLine(lineId);
        //}

        //void _interpreter1_PositionChanged(object sender, PositionEventArgs e)
        //{
        //    if (_lastSecond1 == DateTime.Now.Second)
        //        return;
        //    _visualization.AddPoint(field.GetPositionInField(e.Position), Colors.Blue);
        //    return;
        //}

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {   
            //Distance equipmentWidth = Distance.FromCentimeters(240);
            //_visualization.SetBoundary(new System.Windows.Point(0.0, 0.0), GetPoint(northEast));
            //_visualization.SetEquipmentWidth(equipmentWidth);

            //Position point1 = new Position(new Latitude(58.51288), new Longitude(13.85476));
            //Position point2 = new Position(new Latitude(58.51337), new Longitude(13.85487));
            //line = new Segment(point1, point2);
            //line1 = new Segment(point1.TranslateTo(line.Bearing.Subtract(90), equipmentWidth), point2.TranslateTo(line.Bearing.Subtract(90), equipmentWidth));
            //line2 = new Segment(point1.TranslateTo(line.Bearing.Subtract(90), equipmentWidth.Multiply(2)), point2.TranslateTo(line.Bearing.Subtract(90), equipmentWidth.Multiply(2)));
            //line3 = new Segment(point1.TranslateTo(line.Bearing.Subtract(90), equipmentWidth.Multiply(3)), point2.TranslateTo(line.Bearing.Subtract(90), equipmentWidth.Multiply(3)));
            //int lineAdded = _visualization.AddLine(GetPoint(line.Start), GetPoint(line.End));
            //int lineAdded1 = _visualization.AddLine(GetPoint(line1.Start), GetPoint(line1.End));
            //int lineAdded2 = _visualization.AddLine(GetPoint(line2.Start), GetPoint(line2.End));
            //int lineAdded3 = _visualization.AddLine(GetPoint(line3.Start), GetPoint(line3.End));
            //_visualization.SetTrackingLine(lineAdded);
            //_lightBar.Tolerance = new Distance(50.0, DistanceUnit.Centimeters);
            //_visualization.UpdatePosition(GetPoint(line.Midpoint), line.Bearing.DecimalDegrees);

            //List<Position> points = new List<Position>();
            //points.Add(new Position(new Latitude(58.5127333333333), new Longitude(13.8543533333333)));
            //points.Add(new Position(new Latitude(58.5126866666667), new Longitude(13.85494)));
            //points.Add(new Position(new Latitude(58.5129983333333), new Longitude(13.8550266666667)));
            //points.Add(new Position(new Latitude(58.5130183333333), new Longitude(13.8544166666667)));
            //Boundary boundary = new Boundary(points, Distance.EarthsAverageRadius.Add(Distance.FromMeters(100)));

            //List<Coordinate> coords = new List<Coordinate>();
            //coords.Add(new Coordinate(0, 0));
            //coords.Add(new Coordinate(0, 50));
            //coords.Add(new Coordinate(-50, 50));
            //coords.Add(new Coordinate(-50, 100));
            //coords.Add(new Coordinate(50, 100));
            //coords.Add(new Coordinate(50, 0));
            //coords.Add(new Coordinate(0, 0));
            //if (!DotSpatial.Topology.Algorithm.CgAlgorithms.IsCounterClockwise(coords))
            //    coords.Reverse();
            //PrecisionModel pm = new PrecisionModel(PrecisionModelType.Floating);
            //DotSpatial.Topology.Operation.Buffer.OffsetCurveBuilder curve = new DotSpatial.Topology.Operation.Buffer.OffsetCurveBuilder(pm);
            //IList coordinates = curve.GetRingCurve(coords, DotSpatial.Topology.GeometriesGraph.PositionType.Left, 5);
            //LinearRing ring = new LinearRing((IList<Coordinate>)coordinates[0]);
            //IList coordinates2 = curve.GetRingCurve(ring.Coordinates, DotSpatial.Topology.GeometriesGraph.PositionType.Left, 21.0);
            //LinearRing ring2 = new LinearRing((IList<Coordinate>)coordinates2[0]);
            //for (int i = 0; i < coords.Count - 1; i++)
            //    _visualization.AddLine(new System.Windows.Point(coords[i].X, coords[i].Y), new System.Windows.Point(coords[i + 1].X, coords[i + 1].Y));

            //for (int i = 0; i < ring.Coordinates.Count - 1; i++)
            //    _visualization.AddLine(new System.Windows.Point(ring.Coordinates[i].X, ring.Coordinates[i].Y), new System.Windows.Point(ring.Coordinates[i + 1].X, ring.Coordinates[i + 1].Y));

            //for (int i = 0; i < ring2.Coordinates.Count - 1; i++)
            //    _visualization.AddLine(new System.Windows.Point(ring2.Coordinates[i].X, ring2.Coordinates[i].Y), new System.Windows.Point(ring2.Coordinates[i + 1].X, ring2.Coordinates[i + 1].Y));

            //_visualization.UpdatePosition(new System.Windows.Point(0, 20), 0);
            //Padda traktor = COM3
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
            else if(_actualCoordinate.Distance(_prevTrackCoordinate) > 1.0)
            {
                FarmingGPSLib.FarmingModes.EquipmentTips equipment = _farmingMode.GetEquipmentTips(_actualCoordinate, _actAngle);
                _fieldTracker.AddTrackPoint(equipment.LeftTip, equipment.RightTip);
                _prevTrackCoordinate = _actualCoordinate;
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
            //_receiver = new Piksi(_sbpReceiverSender);
            //_receiver.BasePosition = new Position3D(new Distance(84.0, DistanceUnit.Meters), new Latitude(58.512115), new Longitude(13.85485)).ToCartesianPoint();
            //_receiver.BasePosition = new CartesianPoint(Distance.FromMeters(3242347.98639), Distance.FromMeters(799848.358036), Distance.FromMeters(5415851.61154));
            //_receiver.MinimumSpeedLockHeading = Speed.FromKilometersPerHour(1.0);
            _receiver = new KeyboardSimulator(this, new CartesianPoint(Distance.FromMeters(3242347.98639), Distance.FromMeters(799848.358036), Distance.FromMeters(5415851.61154)).ToPosition3D());
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
            _speedBar.Unit = SpeedUnit.KilometersPerHour;

            NTRIP.Settings.ClientSettings clientSettings = new NTRIP.Settings.ClientSettings();
            clientSettings.IPorHost = "nolgarden.net";
            //clientSettings.IPorHost = "192.168.0.50";
            clientSettings.PortNumber = 5000;
            clientSettings.NTRIPMountPoint = "ExampleName";
            clientSettings.NTRIPUser = new NTRIP.Settings.NTRIPUser("ExAmPlEuSeR", "ExAmPlEpAsSwOrD");
            _ntripClient = new NTRIP.ClientService(clientSettings);
            _ntripClient.StreamDataReceivedEvent += _ntripClient_StreamDataReceivedEvent;
            _ntripClient.Connect();

            //List<Position> test1 = new List<Position>();
            //test1.Add(new Position(new Longitude(13.8548335246), new Latitude(58.5121131874)));
            //test1.Add(test1[0].TranslateTo(Azimuth.East, Distance.FromMeters(500.0)));
            //test1.Add(test1[1].TranslateTo(Azimuth.North, Distance.FromMeters(500.0)));
            //test1.Add(test1[0].TranslateTo(Azimuth.North, Distance.FromMeters(500.0)));
            //test1.Add(new Position(new Longitude(13.8548335246), new Latitude(58.5121131874)));
            //FarmingGPSLib.FieldItems.Field fieldTest = new FarmingGPSLib.FieldItems.Field(test1, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);

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

            //positions.Add(new Position(new Longitude(13.8548335246), new Latitude(58.5121131874)));
            //positions.Add(new Position(new Longitude(13.85472593529563), new Latitude(58.51286418098248)));
            //positions.Add(new Position(new Longitude(13.85480232776614), new Latitude(58.51309394699612)));
            //positions.Add(new Position(new Longitude(13.85459783672272), new Latitude(58.5133243318616)));
            //positions.Add(new Position(new Longitude(13.85426093034614), new Latitude(58.51333741293158)));
            //positions.Add(new Position(new Longitude(13.85375899299726), new Latitude(58.51331511169184)));
            //positions.Add(new Position(new Longitude(13.85376496306266), new Latitude(58.5131940699146)));
            //positions.Add(new Position(new Longitude(13.85410360307241), new Latitude(58.51315106931946)));
            //positions.Add(new Position(new Longitude(13.854119643), new Latitude(58.51301540805415)));
            //positions.Add(new Position(new Longitude(13.8542335246), new Latitude(58.5121131874)));
            //positions.Add(new Position(new Longitude(13.8548335246), new Latitude(58.5121131874)));

            //positions.Add(new Position(new Longitude(13.86279324392395), new Latitude(58.51066382234642)));      
            //positions.Add(new Position(new Longitude(13.85478563941246), new Latitude(58.51136863243741)));
            //positions.Add(new Position(new Longitude(13.85471434441819), new Latitude(58.51103608856224)));
            //positions.Add(new Position(new Longitude(13.85498890430769), new Latitude(58.51066260873715)));
            //positions.Add(new Position(new Longitude(13.85699839016473), new Latitude(58.51036436502054)));
            //positions.Add(new Position(new Longitude(13.85645380687165), new Latitude(58.50885285923857)));
            //positions.Add(new Position(new Longitude(13.85920935486289), new Latitude(58.50860119034701))); 
            //positions.Add(new Position(new Longitude(13.8587278234643), new Latitude(58.50778922546683)));
            //positions.Add(new Position(new Longitude(13.85977443274548), new Latitude(58.50771496122128)));
            //positions.Add(new Position(new Longitude(13.85970095747497), new Latitude(58.50750855559854)));
            //positions.Add(new Position(new Longitude(13.86120034541875), new Latitude(58.50773527788621))); 
            //positions.Add(new Position(new Longitude(13.86279324392395), new Latitude(58.51066382234642)));
            field = new Field(positions, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            FarmingGPSLib.Equipment.Harrow harrow = new FarmingGPSLib.Equipment.Harrow(Distance.FromMeters(6), Distance.FromMeters(1.5), new DotSpatial.Positioning.Angle(180), Distance.FromCentimeters(20));
            //FarmingGPSLib.Equipment.Harrow harrow = new FarmingGPSLib.Equipment.Harrow(Distance.FromMeters(40), Distance.FromMeters(1.5), new DotSpatial.Positioning.Angle(180), Distance.FromCentimeters(20));
            _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(field, harrow, 1);

            _visualization.AddField(field);
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
                _visualization.AddLine(line);
            _visualization.SetEquipmentWidth(harrow.Width);
            //_visualization.UpdatePosition(new Coordinate(farmingMode.TrackingLinesHeadLand[11].Points[0].X, farmingMode.TrackingLinesHeadLand[11].Points[0].Y), 0);
            //farmingMode.CreateTrackingLines(farmingMode.TrackingLinesHeadLand[5]);
            //farmingMode.CreateTrackingLines(farmingMode.TrackingLinesHeadLand[0]);
            DotSpatial.Topology.Angle angle = new DotSpatial.Topology.Angle(0);
            angle.DegreesPos = 99;
            _farmingMode.CreateTrackingLines(field.GetPositionInField(new Position(new Longitude(13.855224), new Latitude(58.512617))), angle);
            //farmingMode.CreateTrackingLines(farmingMode.TrackingLinesHeadLand[1]);
            //Position3D position = new CartesianPoint(Distance.FromMeters(3242347.98639), Distance.FromMeters(799848.358036), Distance.FromMeters(5415851.61154)).ToPosition3D();
            //farmingMode.CreateTrackingLines(field.GetPositionInField(new Position(position.Longitude, position.Latitude)), angle);

            Brush trackinglineColor = Brushes.DarkBlue;
            foreach (TrackingLine line in _farmingMode.TrackingLines)
            {
                _visualization.AddLine(line, trackinglineColor);
                if (trackinglineColor == Brushes.DarkBlue)
                    trackinglineColor = Brushes.Red;
                else if (trackinglineColor == Brushes.Red)
                    trackinglineColor = Brushes.AliceBlue;
                else if (trackinglineColor == Brushes.AliceBlue)
                    trackinglineColor = Brushes.DarkBlue;
            }
            
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

    }
}
