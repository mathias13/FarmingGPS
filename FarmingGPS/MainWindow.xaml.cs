using DotSpatial.Positioning;
using DotSpatial.Topology;
using FarmingGPS.Camera;
using FarmingGPS.Camera.Axis;
using FarmingGPS.Dialogs;
using FarmingGPS.Settings;
using FarmingGPSLib.Settings;
using FarmingGPSLib.Settings.Database;
using FarmingGPSLib.Settings.NTRIP;
using FarmingGPSLib.Settings.Receiver;
using FarmingGPS.Usercontrols;
using FarmingGPS.Usercontrols.Equipments;
using FarmingGPS.Visualization;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.Equipment.BogBalle;
using FarmingGPSLib.Equipment.Vaderstad;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.StateRecovery;
using FarmingGPSLib.Vechile;
using GpsUtilities.Filter;
using GpsUtilities.Reciever;
using log4net;
using NTRIP;
using NTRIP.Settings;
using SwiftBinaryProtocol;
using SwiftBinaryProtocol.Eventarguments;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FarmingGPS.Usercontrols.Events;
using System.Threading;

namespace FarmingGPS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private struct AddObstacleStruct
        {
            public Database.Obstacle.ObstacleType Obstacle;

            public Database.GpsCoordinate Coordinate;
        }

        private struct CoordinateUpdateStruct
        {
            public Coordinate LeftTip { get; set; }

            public Coordinate RightTip { get; set; }

            public Coordinate Center { get; set; }

            public Azimuth Heading { get; set; }

            public bool Reversing { get; set; }
        }

        public struct LightBarUpdateStruct
        {
            public Distance Distance { get; set; }

            public LightBar.Direction Direction { get; set; }
        }

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Constants
        
        protected const double MINIMUM_DISTANCE_BETWEEN_POINTS = 0.5;

        protected const double MAXIMUM_DISTANCE_BETWEEN_POINTS = 5.0;

        protected const double MINIMUM_CHANGE_DIRECTION = 3.0;

        protected const double MAXIMUM_CHANGE_DIRECTION = 10.0;

        protected const double TRACKINGLINE_COVERAGE_DEPLETED = 0.95;

        protected readonly IDictionary<Type, Type> EQUIPMENTCONTROL_VISUALIZATION = new Dictionary<Type, Type>
        {
            {typeof(Calibrator), typeof(BogballeCalibrator) },
            {typeof(Controller), typeof(VaderstadController) }
        };

        #endregion;

        #region Private Variables

        Configuration _config;

        private StateRecoveryManager _stateRecovery;

        private IField _field;
        
        private Database.DatabaseHandler _database;
        
        private string _cameraIp = String.Empty;

        private ClientService _ntripClient;

        private SBPReceiverSender _sbpReceiverSender;

        private IReceiver _receiver;

        private ICamera _camera;
                
        private DateTime _trackingLineEvaluationTimeout = DateTime.MinValue;

        private TrackingLine _activeTrackingLine = null;
        
        private FieldTracker _fieldTracker = new FieldTracker();

        private FieldRateTracker _fieldRateTracker;

        private bool _fieldTrackerActive = false;

        private FieldCreator _fieldCreator;

        private DistanceTrigger _distanceTriggerFieldTracker;

        private int _selectedTrackingLine = -1;

        private FarmingGPSLib.FarmingModes.IFarmingMode _farmingMode;

        private IEquipment _equipment;

        private IVechile _vechile;

        private bool _ntripConnected = false;

        private bool _receiverConnected = false;

        private bool _gpsFixRTK = false;

        private Coordinate _trackLinePointA = null;
        
        private bool _setTrackLineAB = false;

        private bool _startStopAuto = false;

        private System.Threading.Thread _secondaryTasksThread;

        private bool _secondaryTasksThreadStop = false;
               
        private DispatcherTimer _dispatcherTimer;
                
        private Queue<LightBarUpdateStruct> _lightBarQueue = new Queue<LightBarUpdateStruct>();

        private Queue<CoordinateUpdateStruct> _coordinateUpdateStructQueueSecondaryTasks = new Queue<CoordinateUpdateStruct>();

        private Queue<CoordinateUpdateStruct> _coordinateUpdateStructQueueVisual = new Queue<CoordinateUpdateStruct>();

        private bool _addRock = false;

        private Queue<AddObstacleStruct> _obstacleQueue = new Queue<AddObstacleStruct>();

        private object _syncObject = new object();

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 10 });

            Application.Current.Exit += Current_Exit;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            WindowState = WindowState.Maximized;

            _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            ContentRendered += MainWindow_ContentRendered;
            Closing += MainWindow_Closing;

            //Dispatcher timer so that receiver thread is not occupied 
            _dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250.0), DispatcherPriority.Render, new EventHandler(_dispatcherTimer_Tick), Dispatcher);
            _dispatcherTimer.Start();

            //Thread for handling not so important stuff concerning position updates
            _secondaryTasksThread = new System.Threading.Thread(new System.Threading.ThreadStart(SecondaryTasksThread));
            _secondaryTasksThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            _secondaryTasksThread.Start();

            //_camera = new AxisCamera("AxisCase");
            _camera = new AxisCamera(System.Net.IPAddress.Parse("192.168.43.132"));
            _camera.CameraConnectedChangedEvent += _camera_CameraConnectedChangedEvent;
            _camera.CameraImageEvent += _camera_CameraImageEvent;
            SetValue(CameraUnavilableProperty, Visibility.Visible);
                        
            _distanceTriggerFieldTracker = new DistanceTrigger(MINIMUM_DISTANCE_BETWEEN_POINTS, MAXIMUM_DISTANCE_BETWEEN_POINTS, MINIMUM_CHANGE_DIRECTION, MAXIMUM_CHANGE_DIRECTION);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal("Unhandled exception", (Exception)e.ExceptionObject);
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal("Unhandled exception", e.Exception);
        }

        #region DependencyProperties

        protected static readonly DependencyProperty FieldTrackerButtonStyleProperty = DependencyProperty.Register("FieldTrackerButtonStyle", typeof(Style), typeof(MainWindow));

        protected static readonly DependencyProperty FieldTrackerClearButtonVisibility = DependencyProperty.Register("FieldTrackerClearButtonVisibility", typeof(Visibility), typeof(MainWindow));

        protected static readonly DependencyProperty CameraImageProperty = DependencyProperty.Register("CameraImage", typeof(BitmapImage), typeof(MainWindow));

        protected static readonly DependencyProperty CameraSizeProperty = DependencyProperty.Register("CameraSize", typeof(Style), typeof(MainWindow));

        protected static readonly DependencyProperty CameraUnavilableProperty = DependencyProperty.Register("CameraUnavailable", typeof(Visibility), typeof(MainWindow));

        protected static readonly DependencyProperty GPSState = DependencyProperty.Register("GPSState", typeof(bool), typeof(MainWindow));

        protected static readonly DependencyProperty FixModeState = DependencyProperty.Register("FixModeState", typeof(bool), typeof(MainWindow));

        protected static readonly DependencyProperty NTRIPState = DependencyProperty.Register("NTRIPState", typeof(bool), typeof(MainWindow));

        protected static readonly DependencyProperty DBState = DependencyProperty.Register("DBState", typeof(bool), typeof(MainWindow));

        #endregion

        void Current_Exit(object sender, ExitEventArgs e)
        {
            if (_stateRecovery != null)
                _stateRecovery.Dispose();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _fieldTracker.StopTrack();
            _dispatcherTimer.Stop();
            _secondaryTasksThreadStop = true;
           _secondaryTasksThread.Join();
            if (_camera != null)
                if (_camera is IDisposable)
                    (_camera as IDisposable).Dispose();
            if (_ntripClient != null)
                _ntripClient.Dispose();
            if (_receiver != null)
                _receiver.Dispose();
            if (_sbpReceiverSender != null)
                _sbpReceiverSender.Dispose();
            if (_database != null)
                _database.Dispose();

            if (_equipment != null)
                if (_equipment is IDisposable)
                    (_equipment as IDisposable).Dispose();

            YesNoDialog dialog = new YesNoDialog("Vill du radera all sessions data?");
            if (dialog.ShowDialog().Value)
                _stateRecovery.Clear();
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {   
            SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PLAY"));
            SetValue(CameraSizeProperty, (Style)this.FindResource("PiPvideo"));

            SetupSettingsPanel(null);
            
            _speedBar.Unit = SpeedUnit.KilometersPerHour;
            _speedBar.SetSpeed(Speed.FromKilometersPerHour(0.0));
            _workedAreaBar.Unit = WorkedAreaBar.AreaUnitExt.Hectars;
            _fieldTracker.AreaChanged += _fieldTracker_AreaChanged;
            _visualization.AddFieldTracker(_fieldTracker);
            _stateRecovery = new StateRecoveryManager(TimeSpan.FromMinutes(0.5));

            if (_stateRecovery.ObjectsRecovered.Count > 0)
            {
                YesNoDialog dialog = new YesNoDialog("Vill du återställa tidigare tillstånd?");
                if (dialog.ShowDialog().Value)
                {
                    foreach (KeyValuePair<Type, object> recoveredObject in _stateRecovery.ObjectsRecovered)
                    {
                        if (recoveredObject.Key == typeof(Field))
                        {
                            _field = new Field();
                            _field.RestoreObject(recoveredObject.Value);
                            _fieldTracker.FieldToCalculateAreaWithin = _field;
                            _workedAreaBar.SetField(_field);
                            _visualization.SetField(_field);
                            _stateRecovery.AddStateObject(_field);
                            _visualization.AddFieldTracker(_fieldTracker);
                        }
                        else if (recoveredObject.Key == typeof(FieldTracker))
                        {
                            _fieldTracker.RestoreObject(recoveredObject.Value);
                            _stateRecovery.AddStateObject(_fieldTracker);
                            _visualization.AddFieldTracker(_fieldTracker);
                        }
                        else if(recoveredObject.Key.IsSubclassOf(typeof(VechileBase)))
                        {
                            _vechile = Activator.CreateInstance(recoveredObject.Key) as IVechile;
                            _vechile.RestoreObject(recoveredObject.Value);
                        }
                        else if (recoveredObject.Key.IsSubclassOf(typeof(EquipmentBase)))
                        {
                            _equipment = Activator.CreateInstance(recoveredObject.Key) as IEquipment;
                            _equipment.RestoreObject(recoveredObject.Value);
                            if (_equipment is IEquipmentControl)
                                SetIEquipmentControl();

                            _visualization.SetEquipmentWidth(_equipment.Width);
                        }
                        else if (recoveredObject.Key.IsSubclassOf(typeof(FarmingGPSLib.FarmingModes.FarmingModeBase)))
                        {
                            _farmingMode = Activator.CreateInstance(recoveredObject.Key) as FarmingGPSLib.FarmingModes.IFarmingMode;
                            _farmingMode.RestoreObject(recoveredObject.Value);
                            _farmingMode.FarmingEvent += FarmingEvent;
                        }
                        else if(recoveredObject.Key == typeof(FieldRateTracker))
                        {
                            _fieldRateTracker = new FieldRateTracker();
                            BTN_RATE_AUTO.Visibility = Visibility.Visible;
                            _fieldRateTracker.RestoreObject(recoveredObject.Value);
                        }
                    }

                    if(_farmingMode != null)
                    {
                        _visualization.AddLines(_farmingMode.TrackingLinesHeadland.ToArray());
                        _visualization.AddLines(_farmingMode.TrackingLines.ToArray());

                        CheckAllTrackingLines();
                    }
                    if(_equipment != null && _fieldRateTracker != null)
                        if (_equipment is IEquipmentControl)
                            _fieldRateTracker.RegisterEquipmentControl(_equipment as IEquipmentControl);
#if SIM
                    if (_receiver != null)
                        _receiver.Dispose();
                    _receiver = new KeyboardSimulator(this, new Position3D(Distance.FromMeters(0.0),
                        new Longitude(13.855149568), new Latitude(58.5125995962)),
                        false,
                        _vechile.OffsetDirection,
                        _vechile.OffsetDistance,
                        _vechile.WheelAxesDistance);
                    _receiver.BearingUpdate += _receiver_BearingUpdate;
                    _receiver.PositionUpdate += _receiver_PositionUpdate;
                    _receiver.CoordinateUpdate += _receiver_CoordinateUpdate;
                    _receiver.SpeedUpdate += _receiver_SpeedUpdate;
                    _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
                    _receiver.ProjectionInfo = _field.Projection;
#endif
                }
                else
                    _stateRecovery.Clear();
            }
        }
                
        private void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            CoordinateUpdateStruct? newCoord = null;
            LightBarUpdateStruct? newLightBar = null;

            while (_coordinateUpdateStructQueueVisual.Count > 0)
                newCoord = _coordinateUpdateStructQueueVisual.Dequeue();

            while (_lightBarQueue.Count > 0)
                newLightBar = _lightBarQueue.Dequeue();

            if (newCoord.HasValue)
            {
                _visualization.UpdatePosition(newCoord.Value.Center, newCoord.Value.Heading);
                _speedBar.SetSpeed(_receiver.CurrentSpeed);
            }
            
            if (newLightBar.HasValue)
                _lightBar.SetDistance(newLightBar.Value.Distance, newLightBar.Value.Direction);

            SetValue(GPSState, _receiverConnected);
            SetValue(FixModeState, _receiverConnected && _gpsFixRTK);
        }

        private void SecondaryTasksThread()
        {
            while (!_secondaryTasksThreadStop)
            {
                if (_coordinateUpdateStructQueueSecondaryTasks.Count > 0)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    CoordinateUpdateStruct[] coordinates = new CoordinateUpdateStruct[_coordinateUpdateStructQueueSecondaryTasks.Count];
                    for (int i = 0; i < coordinates.Length; i++)
                        coordinates[i] = _coordinateUpdateStructQueueSecondaryTasks.Dequeue();

                    if (_fieldCreator != null)
                    {
                        foreach (var coordinate in coordinates)
                            _fieldCreator.UpdatePosition(coordinate.LeftTip, coordinate.RightTip, coordinate.Heading);
                        if (_fieldCreator.FieldFinished)
                            _fieldCreator = null;
                    }

                    var trackingLineTime = stopwatch.ElapsedMilliseconds;
                    if (DateTime.Now > _trackingLineEvaluationTimeout)
                    {
                        if (_farmingMode != null)
                        {                            
                            double distanceToTrackingLine = double.MaxValue;
                            if (_activeTrackingLine != null)
                                distanceToTrackingLine = _activeTrackingLine.GetDistanceToLine(coordinates[coordinates.Length - 1].Center);
                            if (distanceToTrackingLine > 1.0)
                            {
                                TrackingLine newTrackingLine = _farmingMode.GetClosestLine(coordinates[coordinates.Length - 1].Center, coordinates[coordinates.Length - 1].Heading);
                                if (_activeTrackingLine == null)
                                    _activeTrackingLine = newTrackingLine;
                                else if (!_activeTrackingLine.Equals(newTrackingLine))
                                {
                                    if (_fieldTracker.GetTrackingLineCoverage(_activeTrackingLine) > TRACKINGLINE_COVERAGE_DEPLETED)
                                        _activeTrackingLine.Depleted = true;
                                    else
                                        _activeTrackingLine.Active = false;

                                    newTrackingLine.Active = true;
                                    _activeTrackingLine = newTrackingLine;
                                }
                            }
                        }

                        _trackingLineEvaluationTimeout = DateTime.Now.AddSeconds(3.0);
                    }
                    trackingLineTime = stopwatch.ElapsedMilliseconds - trackingLineTime;

                    var fieldTrackeTime = stopwatch.ElapsedMilliseconds;
                    long testTime = 0;
                    if (coordinates[coordinates.Length - 1].Reversing)
                        _fieldTracker.StopTrack();
                    if (_fieldTracker.IsTracking && !_fieldTrackerActive)
                    {
                        if (coordinates.Length > 1)
                        {
                            FieldTracker.TrackPoint[] trackPoints = new FieldTracker.TrackPoint[coordinates.Length - 1];
                            for (int i = 0; i < trackPoints.Length; i++)
                                trackPoints[i] = new FieldTracker.TrackPoint(coordinates[i].LeftTip, coordinates[i].RightTip);
                            _fieldTracker.AddTrackPoints(trackPoints);
                        }
                        _fieldTracker.StopTrack(new FieldTracker.TrackPoint(coordinates[coordinates.Length - 1].LeftTip, coordinates[coordinates.Length - 1].RightTip));
                    }
                    else if (_fieldTrackerActive && !_fieldTracker.IsTracking && !coordinates[coordinates.Length - 1].Reversing)
                    {
                        _distanceTriggerFieldTracker.Init(coordinates[coordinates.Length - 1].Center, coordinates[coordinates.Length - 1].Heading);
                        _fieldTracker.InitTrack(new FieldTracker.TrackPoint(coordinates[coordinates.Length - 1].LeftTip, coordinates[coordinates.Length - 1].RightTip ));
                    }
                    else if (_fieldTrackerActive && !coordinates[coordinates.Length - 1].Reversing)
                    {
                        List<FieldTracker.TrackPoint> trackPoints = new List<FieldTracker.TrackPoint>();
                        foreach (var coordinate in coordinates)
                        {
                            if (_distanceTriggerFieldTracker.CheckDistance(coordinate.Center, coordinate.Heading))
                                trackPoints.Add(new FieldTracker.TrackPoint(coordinate.LeftTip, coordinate.RightTip ));
                        }
                        testTime = stopwatch.ElapsedMilliseconds;
                        _fieldTracker.AddTrackPoints(trackPoints.ToArray());
                        testTime = stopwatch.ElapsedMilliseconds - testTime;
                    }
                    fieldTrackeTime = stopwatch.ElapsedMilliseconds - fieldTrackeTime;

                    if (stopwatch.ElapsedMilliseconds > 200)
                        Log.Info(String.Format("Secondary tasks took more than 200ms, trakingline:{0}, fieltracker: {1}, testtime: {2}", trackingLineTime.ToString("0ms"), fieldTrackeTime.ToString("0ms"), testTime.ToString("0ms")));
                }
                else if(_obstacleQueue.Count > 0)
                {
                    var obstacles = new List<AddObstacleStruct>();
                    while (_obstacleQueue.Count > 0)
                        obstacles.Add(_obstacleQueue.Dequeue());

                    foreach(var obstacle in obstacles)
                    {
                        if(obstacle.Obstacle == Database.Obstacle.ObstacleType.ROCK)
                            _database.AddRock(obstacle.Coordinate);
                    }
                }
                System.Threading.Thread.Sleep(250);
            }
        }

        #region Private Methods

        private void ShowTrackingLineSettings()
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
                _trackLineGrid.Visibility = Visibility.Visible;
            else
                Dispatcher.Invoke(new Action(ShowTrackingLineSettings), DispatcherPriority.Render);
        }

        private void Camera_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Style fullvideo = (Style)this.FindResource("Fullvideo");
            Style pipvideo = (Style)this.FindResource("PiPvideo");
            if (GetValue(CameraSizeProperty).Equals(pipvideo))
                SetValue(CameraSizeProperty, fullvideo);
            else if (GetValue(CameraSizeProperty).Equals(fullvideo))
                SetValue(CameraSizeProperty, pipvideo);
        }

        private void ToggleFieldTracker()
        {
            _fieldTrackerActive = !_fieldTrackerActive;
            if (_fieldTrackerActive)
            {
                SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PAUSE"));
                SetValue(FieldTrackerClearButtonVisibility, Visibility.Collapsed);
            }
            else
            {
                SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PLAY"));
                SetValue(FieldTrackerClearButtonVisibility, Visibility.Visible);
            }
        }

        private void FarmingEvent(object sender, string e)
        {
            if(e.Contains("EQUIPMENT"))
            {
                if (_equipment is IEquipmentControl && _startStopAuto)
                {
                    IEquipmentControl equipmentControl = _equipment as IEquipmentControl;
                    if (e.Contains("START"))
                        equipmentControl.Start();
                    else if (e.Contains("STOP"))
                        equipmentControl.Stop();
                }
                else
                {
                    if (e.Contains("START"))
                        ShowEventMessage("Starta");
                    else if (e.Contains("STOP"))
                        ShowEventMessage("Stoppa");
                }
            }
        }

        private void ShowEventMessage(string message)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                var timer = new DispatcherTimer(TimeSpan.FromSeconds(10.0), DispatcherPriority.Render, new EventHandler(MessageTimeout), Dispatcher);
                _messageGrid.Children.Add(new EventMessage(message));
                _messageGrid.Visibility = Visibility.Visible;
                timer.Start();
            }
            else
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action<string>(ShowEventMessage), message);
        }

        private void MessageTimeout(object sender, EventArgs e)
        {
            var timer = sender as DispatcherTimer;
            timer.Stop();
            _messageGrid.Visibility = Visibility.Hidden;
            _messageGrid.Children.Clear();
        }
        
        private void CheckAllTrackingLines()
        {
            if (_farmingMode == null)
                return;
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadland)
                if (!line.Depleted)
                    if (_fieldTracker.GetTrackingLineCoverage(line) > TRACKINGLINE_COVERAGE_DEPLETED)
                        line.Depleted = true;

            foreach (TrackingLine line in _farmingMode.TrackingLines)
                if(!line.Depleted)
                    if (_fieldTracker.GetTrackingLineCoverage(line) > TRACKINGLINE_COVERAGE_DEPLETED)
                        line.Depleted = true;
        }
        
        #endregion

        #region Field Events

        private void FieldChoosen(List<Position> e)
        {
            _stateRecovery.RemoveStateObject(_field);
            _stateRecovery.RemoveStateObject(_fieldTracker);
            DotSpatial.Projections.ProjectionInfo projection = HelperClass.GetUtmProjectionZone(e[0]);
            if(_receiver != null)
                _receiver.ProjectionInfo = projection;
            _field = new Field(e, projection);
            _fieldTracker.FieldToCalculateAreaWithin = _field;
            _workedAreaBar.SetField(_field);
            _visualization.SetField(_field);
            
            _settingsGrid.Visibility = Visibility.Hidden;
            _stateRecovery.AddStateObject(_field);
            _stateRecovery.AddStateObject(_fieldTracker);
        }

        private void _fieldTracker_AreaChanged(object sender, AreaChanged e)
        {
            _workedAreaBar.SetWorkedArea(e.Area);
        }

        private void _fieldCreator_FieldCreated(object sender, FieldCreatedEventArgs e)
        {
            _stateRecovery.RemoveStateObject(_field);
            _stateRecovery.RemoveStateObject(_fieldTracker);
            _field = e.Field;
            _fieldTracker.FieldToCalculateAreaWithin = _field;
            _workedAreaBar.SetField(_field);
            _stateRecovery.AddStateObject(_field);
            _stateRecovery.AddStateObject(_fieldTracker);

            _fieldCreator.FieldCreated -= _fieldCreator_FieldCreated;
        }

        #endregion

        #region Camera Events

        private void _camera_CameraImageEvent(object sender, CameraImageEventArgs e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                SetValue(CameraImageProperty, e.BitmapImage);
            }
            else
                Dispatcher.Invoke(DispatcherPriority.Render, new Action<object, CameraImageEventArgs>(_camera_CameraImageEvent), sender, e);
        }

        private void _camera_CameraConnectedChangedEvent(object sender, CameraConnectedEventArgs e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                SetValue(CameraUnavilableProperty, e.Connected ? Visibility.Hidden : Visibility.Visible);
            }
            else
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action<object, CameraConnectedEventArgs>(_camera_CameraConnectedChangedEvent), sender, e);
        }

        #endregion

        #region Database

        private void SetupDatabase(DatabaseConn connSetting)
        {
            if (_database != null)
            {
                _database.DatabaseOnlineChanged -= _database_DatabaseOnlineChanged;
                _database.Dispose();
                _database = null;
            }

            SqlConnectionStringBuilder connString = new SqlConnectionStringBuilder
            {
                Encrypt = connSetting.Encrypt,
                TrustServerCertificate = connSetting.TrustServerCertificate,
                IntegratedSecurity = connSetting.IntegratedSecurity,
                DataSource = connSetting.Url,
                InitialCatalog = connSetting.DatabaseName,
                ConnectTimeout = 5
            };

            UserPasswordDialog userPassDialog = new UserPasswordDialog(connSetting.UserName);
            userPassDialog.ShowDialog();

            connString.UserID = userPassDialog.UserName;
            connString.Password = userPassDialog.Password;

            _database = new Database.DatabaseHandler(connString);
            _database.DatabaseOnlineChanged += _database_DatabaseOnlineChanged;

        }

        private void _database_DatabaseOnlineChanged(object sender, Database.DatabaseOnlineChangedEventArgs e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                SetValue(DBState, e.Online);
            }
            else
                Dispatcher.Invoke(new Action<object, Database.DatabaseOnlineChangedEventArgs>(_database_DatabaseOnlineChanged), DispatcherPriority.Normal, sender, e);
        }

        #endregion

        #region NTRIP
        
        private void SetupNTRIP(ClientSettings settings)
        {
            if(_ntripClient != null)
            {
                _ntripClient.StreamDataReceivedEvent -= _ntripClient_StreamDataReceivedEvent;
                _ntripClient.ConnectionExceptionEvent -= _ntripClient_ConnectionExceptionEvent;
                _ntripClient.Disconnect();
                _ntripClient.Dispose();
                _ntripClient = null;
            }
            _ntripClient = new ClientService(settings);
            _ntripClient.StreamDataReceivedEvent += _ntripClient_StreamDataReceivedEvent;
            _ntripClient.ConnectionExceptionEvent += _ntripClient_ConnectionExceptionEvent;
            _ntripClient.Connect();
        }

        private void _ntripClient_StreamDataReceivedEvent(object sender, NTRIP.Eventarguments.StreamReceivedArgs e)
        {
            if (_sbpReceiverSender != null)
            _sbpReceiverSender.SendMessage(e.DataStream);
            if (!_ntripConnected)
            {
                _ntripConnected = true;
                ChangeNTRIPState();
            }
        }

        private void _ntripClient_ConnectionExceptionEvent(object sender, NTRIP.Eventarguments.ConnectionExceptionArgs e)
        {
            if (_ntripConnected)
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
                SetValue(NTRIPState, _ntripConnected);
            }
            else
                Dispatcher.Invoke(new Action(ChangeNTRIPState), DispatcherPriority.Normal);
        }

        #endregion

        #region Receiver

        private void SetupReceiver(SBPSerial receiver)
        {
            if (!receiver.COMPort.Contains("COM"))
            {
                ComPortDialog comportDialog = new ComPortDialog();
                comportDialog.ShowDialog();

                string comport = comportDialog.ComPort;
                if (comport == String.Empty)
                    comport = "COM1";

                _sbpReceiverSender = new SBPReceiverSender(comport, (int)receiver.Baudrate, receiver.RtsCts);
            }
            else
                _sbpReceiverSender = new SBPReceiverSender(receiver.COMPort, (int)receiver.Baudrate, receiver.RtsCts);

            _sbpReceiverSender.ReadExceptionEvent += _sbpReceiverSender_ReadExceptionEvent;
            if (_receiver != null)
                _receiver.Dispose();
            _receiver = new Piksi(_sbpReceiverSender, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(1000))
            {
                MinimumSpeedLockHeading = Speed.FromKilometersPerHour(1.0)
            };
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.CoordinateUpdate += _receiver_CoordinateUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
        }

        private void _receiver_FixQualityUpdate(object sender, FixQuality fixQuality)
        {
            _gpsFixRTK = fixQuality == FixQuality.FixedRealTimeKinematic;
        }

        private void _receiver_CoordinateUpdate(object sender, Coordinate actualPosition)
        {
            _receiverConnected = true;
            IReceiver receiver = sender as IReceiver;
            if (_field == null || _vechile == null)
                return;

            _vechile.UpdatePosition(receiver);
            
            Coordinate vechileCoordinate = _vechile.CenterRearAxle;
            Coordinate equipmentCoordinate = _vechile.AttachPoint;
            Coordinate leftTip = equipmentCoordinate;
            Coordinate rightTip = equipmentCoordinate;
            Azimuth actualHeading = _vechile.VechileDirection;

            if (_equipment != null)
            {
                leftTip = _equipment.GetLeftTip(equipmentCoordinate, actualHeading);
                rightTip = _equipment.GetRightTip(equipmentCoordinate, actualHeading);
                equipmentCoordinate = _equipment.GetCenter(equipmentCoordinate, actualHeading);
            }

            if (_farmingMode != null && !_vechile.IsReversing)
            {
                _farmingMode.UpdateEvents(equipmentCoordinate, actualHeading);
                _farmingMode.UpdateEvents(new LineString(new Coordinate[2] { leftTip, rightTip }), actualHeading);
            }
            
            if (_fieldRateTracker != null)
                _fieldRateTracker.UpdatePosition(leftTip, rightTip);
            
            if (_activeTrackingLine != null)
            {
                OrientationToLine orientationToLine = _activeTrackingLine.GetOrientationToLine(vechileCoordinate, actualHeading);

                if (_equipment != null)
                {
                    if (_equipment.SideDependent)
                    {
                        _equipment.OppositeSide = (!_farmingMode.EquipmentSideOutRight && !orientationToLine.TrackingBackwards) || (_farmingMode.EquipmentSideOutRight && orientationToLine.TrackingBackwards);
                        _visualization.SetEquipmentOffset(_equipment.OffsetFromVechile, !_equipment.OppositeSide);
                    }
                }

                _activeTrackingLine.Active = true;
                LightBar.Direction direction = LightBar.Direction.Left;
                if (orientationToLine.SideOfLine == OrientationToLine.Side.Left)
                    direction = LightBar.Direction.Right;

                _lightBarQueue.Enqueue(new LightBarUpdateStruct() { Direction = direction, Distance = Distance.FromMeters(orientationToLine.DistanceTo) });
            }

            _coordinateUpdateStructQueueSecondaryTasks.Enqueue(new CoordinateUpdateStruct() { LeftTip = leftTip, RightTip = rightTip, Center = vechileCoordinate, Heading = actualHeading, Reversing = _vechile.IsReversing });
            _coordinateUpdateStructQueueVisual.Enqueue(new CoordinateUpdateStruct() { LeftTip = leftTip, RightTip = rightTip, Center = vechileCoordinate, Heading = actualHeading, Reversing = _vechile.IsReversing });
        }

        private void _receiver_PositionUpdate(object sender, Position actualPosition)
        {
            if (_addRock)
            {
                _obstacleQueue.Enqueue(new AddObstacleStruct() { Obstacle = Database.Obstacle.ObstacleType.ROCK, Coordinate = new Database.GpsCoordinate() { Latitude = actualPosition.Latitude.DecimalDegrees, Longitude = actualPosition.Longitude.DecimalDegrees } });
                _addRock = false;
            }
        }
        
        private void _receiver_BearingUpdate(object sender, Azimuth actualBearing)
        {
        }

        private void _receiver_SpeedUpdate(object sender, Speed actualSpeed)
        {
            if (_equipment != null)
            {
                if (_equipment is IEquipmentControl)
                {
                    double speed = _receiver.RawSpeed.ToKilometersPerHour().Value;
                    if (_vechile != null)
                        if (_vechile.IsReversing)
                            speed = 0.0;

                    ((IEquipmentControl)_equipment).RelaySpeed(speed);
                }
            }
        }

        private void _sbpReceiverSender_ReadExceptionEvent(object sender, SBPReadExceptionEventArgs e)
        {
            Log.Warn("SBPReaderException", e.Exception);
            _receiverConnected = false;
        }

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

        private void BTN_START_FIELD_Click(object sender, RoutedEventArgs e)
        {
            if (_fieldCreator == null)
            {
                if(_equipment == null)
                {
                    OKDialog dialog = new OKDialog("Du måste välja maskin och redskap först");
                    dialog.Show();
                    return;
                }
                _visualization.SetEquipmentWidth(_equipment.Width);
                _fieldTracker.ClearTrack();
                if (_farmingMode != null)
                {
                    _visualization.DeleteLines(_farmingMode.TrackingLines.ToArray());
                    _visualization.DeleteLines(_farmingMode.TrackingLinesHeadland.ToArray());
                }

                DotSpatial.Projections.ProjectionInfo projection = HelperClass.GetUtmProjectionZone(_receiver.CurrentPosition);
                _receiver.ProjectionInfo = projection;

                _fieldCreator = new FieldCreator(projection, FieldCreator.Orientation.Lefthand, _receiver.CurrentPosition);
                _fieldCreator.FieldCreated += _fieldCreator_FieldCreated;
                _field = _fieldCreator.GetField();
                _visualization.AddFieldCreator(_fieldCreator);
                if (!_fieldTrackerActive)
                    ToggleFieldTracker();
            }
        }

        private void BTN_SETTINGS_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsGrid.Visibility == System.Windows.Visibility.Hidden)
                _settingsGrid.Visibility = System.Windows.Visibility.Visible;
            else
                _settingsGrid.Visibility = System.Windows.Visibility.Hidden;
        }

        private void BTN_CHOOSE_TRACKLINE_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTrackingLine > -1)
            {
                _selectedTrackingLine += 1;
                if (_selectedTrackingLine >= _farmingMode.TrackingLinesHeadland.Count)
                    _selectedTrackingLine = 0;
                _visualization.FocusTrackingLine(_farmingMode.TrackingLinesHeadland[_selectedTrackingLine]);
            }
            else
        {
                _selectedTrackingLine = 0;
                _visualization.FocusTrackingLine(_farmingMode.TrackingLinesHeadland[_selectedTrackingLine]);
                BTN_CHOOSE_TRACKLINE.Style = (Style)this.FindResource("BUTTON_MOVE_NEXT");
                BTN_CONFIRM_TRACKLINE.Visibility = Visibility.Visible;
            }
        }

        private void BTN_CONFIRM_TRACKLINE_Click(object sender, RoutedEventArgs e)
        {
            float heading = 0.0f;
            ValueChangeDialog valueChangeDialog = new ValueChangeDialog(0.0f, -360.0f, 360.0f, 5.0f, "0°", "Vinkel från linje");
            if (valueChangeDialog.ShowDialog().Value)
                heading = valueChangeDialog.Value;

            DotSpatial.Topology.Angle headingFromLine = new DotSpatial.Topology.Angle
            {
                DegreesPos = heading
            };

            BTN_CHOOSE_TRACKLINE.Style = (Style)this.FindResource("BUTTON_CHOOSE_TRACKLINE");
            BTN_CONFIRM_TRACKLINE.Visibility = Visibility.Collapsed;
            _trackLineGrid.Visibility = Visibility.Collapsed;
            _visualization.CancelFocus();
            foreach (var trackingLine in _farmingMode.TrackingLinesHeadland)
                _visualization.DeleteLine(trackingLine);

            if (_equipment.SideDependent)
                _farmingMode.CreateTrackingLines(_farmingMode.TrackingLinesHeadland[_selectedTrackingLine], headingFromLine, _equipment.OffsetFromVechile.ToMeters().Value);
            else
                _farmingMode.CreateTrackingLines(_farmingMode.TrackingLinesHeadland[_selectedTrackingLine], headingFromLine);

            _visualization.AddLines(_farmingMode.TrackingLinesHeadland.ToArray());
            _visualization.AddLines(_farmingMode.TrackingLines.ToArray());
            _visualization.CancelFocus();
            _selectedTrackingLine = -1;
        }

        private void BTN_PLAY_TRACKER_Click(object sender, RoutedEventArgs e)
        {
            ToggleFieldTracker();
        }

        private void BTN_CLEAR_TRACKER_Click(object sender, RoutedEventArgs e)
        {
            _fieldTracker.ClearTrack();
        }

        private void BTN_SET_TRACKINGLINE_AB_Click(object sender, RoutedEventArgs e)
        {
            if(!_setTrackLineAB)
            {
                BTN_SET_TRACKINGLINE_AB.Style = (Style)this.FindResource("BUTTON_TRACKLINE_A");
                _setTrackLineAB = true;
            }
            else
            {
                if(_trackLinePointA == null)
                {
                    _trackLinePointA = _vechile.CenterRearAxle;
                    BTN_SET_TRACKINGLINE_AB.Style = (Style)this.FindResource("BUTTON_TRACKLINE_B");
                }
                else
                {
                    var trackLineB = _vechile.CenterRearAxle;
                    _farmingMode.CreateTrackingLines(_trackLinePointA, trackLineB);
                    _visualization.AddLines(_farmingMode.TrackingLines.ToArray());
                    _trackLinePointA = null;
                    _setTrackLineAB = false;
                    BTN_SET_TRACKINGLINE_AB.Style = (Style)this.FindResource("BUTTON_TRACKLINE_AB");
                    _trackLineGrid.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BTN_EQUIPMENT_Click(object sender, RoutedEventArgs e)
        {
            if (_equipmentGrid.Visibility == System.Windows.Visibility.Hidden)
                _equipmentGrid.Visibility = System.Windows.Visibility.Visible;
            else
                _equipmentGrid.Visibility = System.Windows.Visibility.Hidden;
        }

        private void BTN_START_STOP_AUTO_Click(object sender, RoutedEventArgs e)
        {
            _startStopAuto = BTN_START_STOP_AUTO.IsChecked.Value;
        }

        private void BTN_START_EQUIPMENT_Click(object sender, RoutedEventArgs e)
        {
            if (_equipment is IEquipmentControl)
                (_equipment as IEquipmentControl).Start();
        }

        private void BTN_STOP_EQUIPMENT_Click(object sender, RoutedEventArgs e)
        {
            if (_equipment is IEquipmentControl)
                (_equipment as IEquipmentControl).Stop();
        }

        private void BTN_RATE_AUTO_Click(object sender, RoutedEventArgs e)
        {
            _fieldRateTracker.Auto = BTN_RATE_AUTO.IsChecked.Value;
        }

        private void BTN_MARKER_Click(object sender, RoutedEventArgs e)
        {
            _addRock = true;
        }

        #endregion

        #region Settings

        private void SetupSettingsPanel(SettingsCollection settings)
        {
            ClientSettingsExt sectionNTRIP = (ClientSettingsExt)_config.Sections[typeof(ClientSettingsExt).FullName];
            ISettingsCollection ntripClient;
            if (sectionNTRIP != null)
            {
                ntripClient = new ClientSettingsExt(sectionNTRIP);
                SetupNTRIP(sectionNTRIP);
            }
            else
                ntripClient = new ClientSettingsExt();

            SBPSerial sectionReceiver = (SBPSerial)_config.Sections[typeof(SBPSerial).FullName];
            ISettingsCollection receiver;
            if (sectionReceiver != null)
            {
                receiver = new SBPSerial(sectionReceiver);
                SetupReceiver(sectionReceiver);
            }
            else
                receiver = new SBPSerial();

            DatabaseConn sectionDatabase = (DatabaseConn)_config.Sections[typeof(DatabaseConn).FullName];
            ISettingsCollection database;
            if (sectionDatabase != null)
            {
                database = new DatabaseConn(sectionDatabase);
                SetupDatabase(sectionDatabase);
            }
            else
                database = new DatabaseConn();

            ISettingsCollection visual = new SettingsCollection("Visualisering");
            visual.ChildSettings.Add(new FarmingGPS.Visualization.Settings.LightBar());
            SettingGroup lightBarGroup = new SettingGroup(visual.ChildSettings[0].Name, null, new SettingsCollectionControl(visual.ChildSettings[0]));
            (lightBarGroup.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
            SettingGroup visualGroup = new SettingGroup("Visualisering", new SettingGroup[] { lightBarGroup }, null);

            ISettingsCollection connections = new SettingsCollection("Anslutningar");
            connections.ChildSettings.Add(ntripClient);
            connections.ChildSettings.Add(database);
            connections.ChildSettings.Add(receiver);
            SettingGroup ntripGroup = new SettingGroup(connections.ChildSettings[0].Name, null, new SettingsCollectionControl(connections.ChildSettings[0]));
            (ntripGroup.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
            SettingGroup databaseGroup = new SettingGroup(connections.ChildSettings[1].Name, null, new SettingsCollectionControl(connections.ChildSettings[1]));
            (databaseGroup.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
            SettingGroup receiverGroup = new SettingGroup(connections.ChildSettings[2].Name, null, new SettingsCollectionControl(connections.ChildSettings[2]));
            (receiverGroup.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
            SettingGroup connectionGroup = new SettingGroup(connections.Name, new SettingGroup[] { ntripGroup, databaseGroup, receiverGroup}, new SettingsCollectionControl(connections));

            SettingGroup field = new SettingGroup("Fält", null, new GetField());
            (field.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;

            SettingGroup farmingMode = new SettingGroup("Bearbetningläge", null, new FarmingMode());
            (farmingMode.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;

            SettingGroup equipmentRate = new SettingGroup("Redskapsstyrning", null, new GetEquipmentRate());
            (equipmentRate.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
            (equipmentRate.SettingControl as ISettingsChanged).RegisterSettingEvent((field.SettingControl as ISettingsChanged));

            SettingGroup redskap = new SettingGroup("Redskap", new SettingGroup[] { farmingMode, equipmentRate}, new GetVechileEquipment());
            (redskap.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
            (equipmentRate.SettingControl as ISettingsChanged).RegisterSettingEvent((redskap.SettingControl as ISettingsChanged));

            SettingGroup settingRoot = new SettingGroup("Inställningar", new SettingGroup[] { connectionGroup, field, redskap, visualGroup }, null);
            _settingsTree.ItemsSource = settingRoot;
        }

        private void _settingsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_settingsTree.SelectedItem is SettingGroup)
            {
                SettingGroup settingItem = _settingsTree.SelectedItem as SettingGroup;
                if (settingItem.SettingControl is IDatabaseSettings && _database != null)
                    (settingItem.SettingControl as IDatabaseSettings).RegisterDatabaseHandler(_database);
                _settingsUsercontrolGrid.Children.Clear();
                if(settingItem.SettingControl != null)
                    _settingsUsercontrolGrid.Children.Add(settingItem.SettingControl);
            }

        }

        private void SettingItem_SettingChanged(object sender, string e)
        {
            if (sender is GetField)
                GetFieldChanged((sender as GetField), e);
            else if (sender is GetVechileEquipment)
                SetVechileEquipment((sender as GetVechileEquipment));
            else if (sender is FarmingMode)
                SetFarmingMode((sender as FarmingMode));
            else if (sender is GetEquipmentRate)
                SetEquipmentRate((sender as GetEquipmentRate));
            else if (sender is SettingsCollectionControl)
            {
                SettingsCollectionControl settingControl = sender as SettingsCollectionControl;
                if (settingControl.Settings is ConfigurationSection)
                {
                    ConfigurationSection configSection = _config.Sections[settingControl.Settings.GetType().FullName];
                    if (configSection == null)
                        _config.Sections.Add(settingControl.Settings.GetType().FullName, settingControl.Settings as ConfigurationSection);
                    else if (!configSection.Equals(settingControl.Settings))
                    {
                        _config.Sections.Remove(settingControl.Settings.GetType().FullName);
                        _config.Sections.Add(settingControl.Settings.GetType().FullName, settingControl.Settings as ConfigurationSection);
                    }

                    _config.Save(ConfigurationSaveMode.Modified, true);
                }
                if (settingControl.Settings is DatabaseConn)
                    SetupDatabase(settingControl.Settings as DatabaseConn);
                else if (settingControl.Settings is ClientSettingsExt)
                    SetupNTRIP(settingControl.Settings as ClientSettings);
                else if (settingControl.Settings is SBPSerial)
                    SetupReceiver(settingControl.Settings as SBPSerial);
                else if (settingControl.Settings is Visualization.Settings.LightBar)
                    _lightBar.Tolerance = new Distance((settingControl.Settings as Visualization.Settings.LightBar).Tolerance, DistanceUnit.Centimeters);
                else if (_equipment is IEquipmentControl)
                {
                    IEquipmentControl equipmentControl = _equipment as IEquipmentControl;
                    if (settingControl.Settings.GetType() == equipmentControl.ControllerSettingsType)
                        equipmentControl.RegisterController(settingControl.Settings);
                }
            }
        }

        private void SetEquipmentRate(GetEquipmentRate usercontrol)
        {
            if (_fieldRateTracker != null)
                _stateRecovery.RemoveStateObject(_fieldRateTracker);

            BTN_RATE_AUTO.Visibility = Visibility.Visible;
            _fieldRateTracker = new FieldRateTracker(usercontrol.DefaultRate, 5.0, usercontrol.ShapeFile, 1, _field);
            _stateRecovery.AddStateObject(_fieldRateTracker);
            if (_equipment is IEquipmentControl)
                _fieldRateTracker.RegisterEquipmentControl(_equipment as IEquipmentControl);
        }

        private void SetVechileEquipment(GetVechileEquipment userControl)
        {
            _stateRecovery.RemoveStateObject(_equipment);

            _vechile = new Tractor(new Azimuth(userControl.Vechile.ReceiverAngleFromCenter), 
                Distance.FromMeters(userControl.Vechile.ReceiverDistFromCenter), 
                Distance.FromMeters(userControl.Vechile.WheelAxesDist),
                Distance.FromMeters(userControl.VechileAttach.AttachDistFromCenter),
                new Azimuth(userControl.VechileAttach.AttachAngleFromCenter));
            
#if SIM
            if (_receiver != null)
                _receiver.Dispose();
            _receiver = new KeyboardSimulator(this, new Position3D(Distance.FromMeters(0.0), 
                new Longitude(13.855149568), new Latitude(58.5125995962)), 
                false, 
                new Azimuth(userControl.Vechile.ReceiverAngleFromCenter), 
                Distance.FromMeters(userControl.Vechile.ReceiverDistFromCenter), 
                Distance.FromMeters(userControl.Vechile.WheelAxesDist));
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.CoordinateUpdate += _receiver_CoordinateUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
            if (_field != null)
                _receiver.ProjectionInfo = _field.Projection;
#endif

            _stateRecovery.AddStateObject(_vechile);

            _stateRecovery.RemoveStateObject(_equipment);

            if (_equipment != null)
                if (_equipment is IDisposable)
                    (_equipment as IDisposable).Dispose();

            if (userControl.Equipment.EquipmentClass == null)
            {
                _equipment = new Harrow(Distance.FromMeters(userControl.Equipment.WorkWidth), Distance.FromMeters(userControl.Equipment.DistFromAttach), new Azimuth(userControl.Equipment.AngleFromAttach), _vechile);
            }
            else
            {
                try
                {
                    Type equipmentClass = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()).Where(t => String.Equals(t.FullName, userControl.Equipment.EquipmentClass, StringComparison.Ordinal)).First();
                    if (equipmentClass == null)
                        _equipment = new Harrow(Distance.FromMeters(userControl.Equipment.WorkWidth), Distance.FromMeters(userControl.Equipment.DistFromAttach), new Azimuth(userControl.Equipment.AngleFromAttach), _vechile);
                    else if (equipmentClass.IsAssignableFrom(typeof(IEquipment)))
                        _equipment = (IEquipment)Activator.CreateInstance(equipmentClass, Distance.FromMeters(userControl.Equipment.WorkWidth), Distance.FromMeters(userControl.Equipment.DistFromAttach), new Azimuth(userControl.Equipment.AngleFromAttach), _vechile);
                    else
                        _equipment = (IEquipment)Activator.CreateInstance(equipmentClass, Distance.FromMeters(userControl.Equipment.WorkWidth), Distance.FromMeters(userControl.Equipment.DistFromAttach), new Azimuth(userControl.Equipment.AngleFromAttach), _vechile);

                    if (_equipment is IEquipmentControl)
                        SetIEquipmentControl();

                }
                catch (Exception e)
                {
                    Log.Error("Failed to set equipment", e);
                    return;
                }

                _stateRecovery.AddStateObject(_equipment);
                _visualization.SetEquipmentWidth(_equipment.Width);
            }
        }

        private void SetIEquipmentControl()
        {
            IEquipmentControl equipmentControl = _equipment as IEquipmentControl;
            object settings = _config.Sections[equipmentControl.ControllerSettingsType.FullName];
            if (settings == null)
            {
                settings = Activator.CreateInstance(equipmentControl.ControllerSettingsType);
                _config.Sections.Add(equipmentControl.ControllerSettingsType.FullName, settings as ConfigurationSection);
                _config.Save();
            }

            object controller = equipmentControl.RegisterController(settings);

            SettingGroup settingRoot = _settingsTree.ItemsSource as SettingGroup;

            bool alreadyInTree = false;
            foreach (SettingGroup setting in settingRoot.Items[0])
                if (setting.Name == ((ISettingsCollection)settings).Name)
                    alreadyInTree = true;
            if (!alreadyInTree)
            {
                ISettingsCollection settingsCollection = (ISettingsCollection)Activator.CreateInstance(equipmentControl.ControllerSettingsType, new object[] { settings });
                SettingGroup controllerSettings = new SettingGroup(settingsCollection.Name, null, new SettingsCollectionControl(settingsCollection));
                (controllerSettings.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
                ((SettingsCollectionControl)settingRoot.Items[0].SettingControl).Settings.ChildSettings.Add(settingsCollection);
                settingRoot.Items[0].Items.Add(controllerSettings);
                _settingsTree.ItemsSource = null;
                _settingsTree.ItemsSource = settingRoot;
            }

            if (EQUIPMENTCONTROL_VISUALIZATION.ContainsKey(equipmentControl.ControllerType))
            {
                PANEL_START_STOP_AUTO.Visibility = Visibility.Visible;
                BTN_EQUIPMENT.Visibility = Visibility.Visible;
                while (_equipmentControlGrid.Children.Count > 1)
                    _equipmentControlGrid.Children.RemoveAt(1);
                _equipmentControlGrid.Children.Add(Activator.CreateInstance(EQUIPMENTCONTROL_VISUALIZATION[equipmentControl.ControllerType], controller) as UserControl);
                if (_equipment is IEquipmentStat)
                {
                    while (_equipmentStatGrid.Children.Count > 1)
                        _equipmentStatGrid.Children.RemoveAt(1);
                    _equipmentStatGrid.Children.Add(new EquipmentStat((IEquipmentStat)_equipment));
                    _equipmentLevelBar.Visibility = Visibility.Visible;
                    _equipmentLevelBar.RegisterEquipmentStat((IEquipmentStat)_equipment);
                }
                else
                {
                    _equipmentLevelBar.Visibility = Visibility.Hidden;
                    _equipmentStatGrid.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                PANEL_START_STOP_AUTO.Visibility = Visibility.Hidden;
                BTN_EQUIPMENT.Visibility = Visibility.Hidden;
                _equipmentGrid.Visibility = Visibility.Hidden;
                _equipmentStatGrid.Visibility = Visibility.Hidden;
            }
        }

        private void SetFarmingMode(FarmingMode userControl)
        {
            if(_field == null)
            {
                OKDialog dialog = new OKDialog("Du måste välja fält först");
                dialog.Show();
                return;
            }
            
            _stateRecovery.RemoveStateObject(_farmingMode);
            if (_equipment == null)
                return;

            if (_farmingMode != null)
            {
                _visualization.DeleteLines(_farmingMode.TrackingLines.ToArray());
                _visualization.DeleteLines(_farmingMode.TrackingLinesHeadland.ToArray());
            }
            
            _equipment.Overlap = Distance.FromMeters(userControl.Overlap);
            if (_equipment.FarmingMode == null)
            {
                if(userControl.HeadLandWidthUsed)
                    _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, _equipment, userControl.HeadLandWidth);
                else
                    _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, _equipment, userControl.Headlands);
            }
            else
            {
                try
                {
                    if (userControl.HeadLandWidthUsed)
                        _farmingMode = (FarmingGPSLib.FarmingModes.IFarmingMode)Activator.CreateInstance(_equipment.FarmingMode, _field, _equipment, userControl.HeadLandWidth);
                    else
                        _farmingMode = (FarmingGPSLib.FarmingModes.IFarmingMode)Activator.CreateInstance(_equipment.FarmingMode, _field, _equipment, userControl.Headlands);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to set farmingmode", e);
                    return;
                }
            }
            if (_equipment.SideDependent)
                _farmingMode.EquipmentSideOutRight = userControl.EquipmentSideOutRight;

            _visualization.AddLines(_farmingMode.TrackingLinesHeadland.ToArray());
            _visualization.AddLines(_farmingMode.TrackingLines.ToArray());

            YesNoDialog dialogFieldTracker = new YesNoDialog("Vill du radera körd area?");
            if (dialogFieldTracker.ShowDialog().Value)
            {
                if (_fieldTrackerActive)
                    ToggleFieldTracker();
                _fieldTracker.ClearTrack();
            }

            CheckAllTrackingLines();

            _stateRecovery.AddStateObject(_farmingMode);

            _settingsGrid.Visibility = Visibility.Hidden;
            ShowTrackingLineSettings();
            _farmingMode.FarmingEvent += FarmingEvent;
        }

        private void GetFieldChanged(GetField userControl, string eventName)
        {
            switch (eventName)
            {
                case GetField.FIELD_CHOOSEN:
                    FieldChoosen(userControl.FieldChoosen);
                    break;
            }
        }

        #endregion

    }
}
