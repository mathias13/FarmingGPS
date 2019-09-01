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
using System.Windows.Threading;


namespace FarmingGPS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Constants
        
        protected const double MINIMUM_DISTANCE_BETWEEN_POINTS = 0.5;

        protected const double MAXIMUM_DISTANCE_BETWEEN_POINTS = 5.0;

        protected const double MINIMUM_CHANGE_DIRECTION = 3.0;

        protected const double MAXIMUM_CHANGE_DIRECTION = 10.0;

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
        
        private Database.Equipment _equipmentChoosen;

        private bool _ntripConnected = false;

        private Position _trackLinePointA = Position.Empty;
        
        private bool _setTrackLineAB = false;

        private bool _startStopAuto = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.Exit += Current_Exit;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            WindowState = WindowState.Maximized;

            _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            ContentRendered += MainWindow_ContentRendered;
            Closing += MainWindow_Closing;

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

        protected static readonly DependencyProperty FixMode = DependencyProperty.Register("FixMode", typeof(string), typeof(MainWindow));

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
            if (_equipment != null)
                if (_equipment is IDisposable)
                    (_equipment as IDisposable).Dispose();
            if (_camera != null)
                if (_camera is IDisposable)
                    (_camera as IDisposable).Dispose();
            if (_ntripClient != null)
                _ntripClient.Dispose();
            if (_receiver != null)
                if (_receiver is IDisposable)
                    (_receiver as IDisposable).Dispose();
            if (_sbpReceiverSender != null)
                _sbpReceiverSender.Dispose();
            if (_database != null)
                _database.Dispose();

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
                        }
                        if (recoveredObject.Key == typeof(FieldTracker))
                        {
                            _fieldTracker.RestoreObject(recoveredObject.Value);
                            _stateRecovery.AddStateObject(_fieldTracker);
                        }
                    }
                }
            }

#if DEBUG
            _receiver = new KeyboardSimulator(this, new Position3D(Distance.FromMeters(0.0), new Longitude(13.855032), new Latitude(58.512722)), false);
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;

            if (_field == null)
            {
                List<Position> positions = new List<Position>();
                positions.Add(new Position(new Latitude(58.512722), new Longitude(13.855032)));
                positions.Add(new Position(new Latitude(58.513399), new Longitude(13.855150)));
                positions.Add(new Position(new Latitude(58.513462), new Longitude(13.854345)));
                positions.Add(new Position(new Latitude(58.512865), new Longitude(13.854194)));
                positions.Add(new Position(new Latitude(58.512722), new Longitude(13.855032)));
                FieldChoosen(positions);
            }

#endif

        }

        private void _sbpReceiverSender_ReadExceptionEvent(object sender, SBPReadExceptionEventArgs e)
        {
            Log.Warn("SBPReaderException", e.Exception);
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
            }
        }
        
        private void CheckAllTrackingLines()
        {
            if (_farmingMode == null)
                return;
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadland)
                if (!line.Depleted)
                    if (_fieldTracker.GetTrackingLineCoverage(line) > 0.97)
                        line.Depleted = true;

            foreach (TrackingLine line in _farmingMode.TrackingLines)
                if(!line.Depleted)
                    if (_fieldTracker.GetTrackingLineCoverage(line) > 0.97)
                        line.Depleted = true;
        }
        
        #endregion

        #region Field Events

        private void FieldChoosen(List<Position> e)
        {
            _stateRecovery.RemoveStateObject(_field);
            _stateRecovery.RemoveStateObject(_fieldTracker);
            _field = new Field(e, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
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
            _field = e.Field;
            _fieldTracker.FieldToCalculateAreaWithin = _field;
            _workedAreaBar.SetField(_field);
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
                Dispatcher.Invoke(DispatcherPriority.Render, new Action<object, CameraConnectedEventArgs>(_camera_CameraConnectedChangedEvent), sender, e);
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

            SqlConnectionStringBuilder connString = new SqlConnectionStringBuilder();
            connString.Encrypt = connSetting.Encrypt;
            connString.TrustServerCertificate = connSetting.TrustServerCertificate;
            connString.IntegratedSecurity = connSetting.IntegratedSecurity;
            connString.DataSource = connSetting.Url;
            connString.InitialCatalog = connSetting.DatabaseName;
            connString.ConnectTimeout = 5;

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
                Dispatcher.Invoke(new Action<object, Database.DatabaseOnlineChangedEventArgs>(_database_DatabaseOnlineChanged), System.Windows.Threading.DispatcherPriority.Render, sender, e);
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
                Dispatcher.Invoke(new Action(ChangeNTRIPState), System.Windows.Threading.DispatcherPriority.Render);
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
            _receiver = new Piksi(_sbpReceiverSender, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(1000));
            _receiver.MinimumSpeedLockHeading = Speed.FromKilometersPerHour(2.0);
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
        }

        private void _receiver_FixQualityUpdate(object sender, FixQuality fixQuality)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                SetValue(FixMode, fixQuality.ToString());
                SetValue(FixModeState, fixQuality == FixQuality.FixedRealTimeKinematic);
            }
            else
                Dispatcher.Invoke(new Action<object, FixQuality>(_receiver_FixQualityUpdate), System.Windows.Threading.DispatcherPriority.Render, this, fixQuality);
        }

        private void _receiver_PositionUpdate(object sender, Position actualPosition)
        {
            IReceiver receiver = sender as IReceiver;
            if (_field == null)
                return;
            Coordinate actualCoordinate;
            if (_equipment == null)
                actualCoordinate = _field.GetPositionInField(actualPosition);
            else
                actualCoordinate = _field.GetPositionInField(_equipment.GetCenter(actualPosition, receiver.CurrentBearing));


            Azimuth actualHeading = receiver.CurrentBearing;
            _visualization.UpdatePosition(actualCoordinate, actualHeading);
            if(_farmingMode != null)
                _farmingMode.UpdateEvents(actualCoordinate, actualHeading);
            if (_equipment != null)
            {
                Coordinate leftTip = _field.GetPositionInField(_equipment.GetLeftTip(actualPosition, actualHeading));
                Coordinate rightTip = _field.GetPositionInField(_equipment.GetRightTip(actualPosition, actualHeading));

                if (_fieldRateTracker != null)
                    _fieldRateTracker.UpdatePosition(leftTip, rightTip);

                if (_fieldTracker.IsTracking && !_fieldTrackerActive)
                    _fieldTracker.StopTrack(leftTip, rightTip);
                else if (_fieldTrackerActive && !_fieldTracker.IsTracking)
                {
                    _distanceTriggerFieldTracker.Init(actualPosition, actualHeading);
                    _fieldTracker.InitTrack(leftTip, rightTip);
                }
                else if (_fieldTrackerActive && _distanceTriggerFieldTracker.CheckDistance(actualPosition, actualHeading))
                    _fieldTracker.AddTrackPoint(leftTip, rightTip);
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
                        if (_fieldTracker.GetTrackingLineCoverage(_activeTrackingLine) > 0.97)
                            _activeTrackingLine.Depleted = true;

                        _activeTrackingLine.Active = false;
                        _activeTrackingLine = newTrackingLine;
                    }
                }
                
                //TODO Make this a setting instead 
                _trackingLineEvaluationTimeout = DateTime.Now.AddSeconds(3.0);
            }
            
            if (_activeTrackingLine != null)
            {
                OrientationToLine orientationToLine;
                if (_farmingMode.TrackingLinesHeadland.Contains(_activeTrackingLine))
                    orientationToLine = _activeTrackingLine.GetOrientationToLine(actualCoordinate, actualHeading, false);
                else
                    orientationToLine = _activeTrackingLine.GetOrientationToLine(actualCoordinate, actualHeading, true);
                _activeTrackingLine.Active = true;
                LightBar.Direction lightBarDirection = LightBar.Direction.Left;
                if (orientationToLine.SideOfLine == OrientationToLine.Side.Left)
                    lightBarDirection = LightBar.Direction.Right;

                _lightBar.SetDistance(Distance.FromMeters(orientationToLine.DistanceTo), lightBarDirection);
            }
        }
        
        private void _receiver_BearingUpdate(object sender, Azimuth actualBearing)
        {
        }

        private void _receiver_SpeedUpdate(object sender, Speed actualSpeed)
        {
            _speedBar.SetSpeed(actualSpeed);
            if (_equipment is IEquipmentControl)
                ((IEquipmentControl)_equipment).RelaySpeed(actualSpeed.ToKilometersPerHour().Value);
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
                _fieldCreator = new FieldCreator(DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N, FieldCreator.Orientation.Lefthand, _receiver, _equipment);
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
            BTN_CHOOSE_TRACKLINE.Style = (Style)this.FindResource("BUTTON_CHOOSE_TRACKLINE");
            BTN_CONFIRM_TRACKLINE.Visibility = Visibility.Collapsed;
            _trackLineGrid.Visibility = Visibility.Collapsed;
            _farmingMode.CreateTrackingLines(_farmingMode.TrackingLinesHeadland[_selectedTrackingLine]);
            foreach (TrackingLine trackingLine in _farmingMode.TrackingLines)
                _visualization.AddLine(trackingLine);
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
                if(_trackLinePointA.IsEmpty)
                {
                    _trackLinePointA = _receiver.CurrentPosition;
                    BTN_SET_TRACKINGLINE_AB.Style = (Style)this.FindResource("BUTTON_TRACKLINE_B");
                }
                else
                {
                    Position trackLineB = _receiver.CurrentPosition;
                    _farmingMode.CreateTrackingLines(_field.GetPositionInField(_trackLinePointA), _field.GetPositionInField(trackLineB));
                    foreach (TrackingLine trackingLine in _farmingMode.TrackingLines)
                        _visualization.AddLine(trackingLine);
                    _trackLinePointA = Position.Empty;
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

        private void BTN_RATE_AUTO_Click(object sender, RoutedEventArgs e)
        {
            _fieldRateTracker.Auto = BTN_RATE_AUTO.IsChecked.Value;
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

            DatabaseConn sectionDatabase = (DatabaseConn)_config.Sections[typeof(DatabaseConn).FullName];
            ISettingsCollection database;
            if (sectionDatabase != null)
            {
                database = new DatabaseConn(sectionDatabase);
                SetupDatabase(sectionDatabase);
            }
            else
                database = new DatabaseConn();
            
            SBPSerial sectionReceiver = (SBPSerial)_config.Sections[typeof(SBPSerial).FullName];
            ISettingsCollection receiver;
            if (sectionReceiver != null)
            {
                receiver = new SBPSerial(sectionReceiver);
                SetupReceiver(sectionReceiver);
            }
            else
                receiver = new SBPSerial();

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
                GetVechileEquipment((sender as GetVechileEquipment));
            else if (sender is FarmingMode)
                SetEquipmentAndFarmingMode((sender as FarmingMode));
            else if (sender is GetEquipmentRate)
                SetEquipmentRate((sender as GetEquipmentRate));
            else if (sender is SettingsCollectionControl)
            {
                SettingsCollectionControl settingControl = sender as SettingsCollectionControl;
                if (settingControl.Settings is ConfigurationSection)
                {
                    ConfigurationSection configSection = _config.Sections[settingControl.Settings.GetType().FullName];
                    if (configSection != null)
                        _config.Sections.Remove(settingControl.Settings.GetType().FullName);

                    _config.Sections.Add(settingControl.Settings.GetType().FullName, settingControl.Settings as ConfigurationSection);
                    _config.Save(ConfigurationSaveMode.Modified, true);
                }
                if (settingControl.Settings is DatabaseConn)
                    SetupDatabase(settingControl.Settings as DatabaseConn);
                else if (settingControl.Settings is ClientSettingsExt)
                    SetupNTRIP(settingControl.Settings as ClientSettings);
                else if (settingControl.Settings is SBPSerial)
                    SetupReceiver(settingControl.Settings as SBPSerial);
                else if (settingControl.Settings is Visualization.Settings.LightBar)
                    _lightBar.Tolerance = new Distance((settingControl.Settings as Visualization.Settings.LightBar).Tolerance, DistanceUnit.Meters);
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
            BTN_RATE_AUTO.Visibility = Visibility.Visible;
            _fieldRateTracker = new FieldRateTracker(usercontrol.DefaultRate, 5.0, usercontrol.ShapeFile, 1, _field);
            if (_equipment is IEquipmentControl)
                _fieldRateTracker.RegisterEquipmentControl(_equipment as IEquipmentControl);
        }

        private void SetEquipmentAndFarmingMode(FarmingMode userControl)
        {
            if(_field == null)
            {
                OKDialog dialog = new OKDialog("Du måste välja fält först");
                dialog.Show();
                return;
            }
            
            _stateRecovery.RemoveStateObject(_farmingMode);
            if (_equipmentChoosen == null)
                return;

            if (_farmingMode != null)
            {
                foreach (TrackingLine trackingLine in _farmingMode.TrackingLines)
                    _visualization.DeleteLine(trackingLine);
                foreach (TrackingLine trackingLine in _farmingMode.TrackingLinesHeadland)
                    _visualization.DeleteLine(trackingLine);
            }

            if (_equipment != null)
                if (_equipment is IDisposable)
                    (_equipment as IDisposable).Dispose();

            if (_equipmentChoosen.EquipmentClass == null)
            {
                _equipment = new Harrow(Distance.FromMeters(_equipmentChoosen.WorkWidth), Distance.FromMeters(_equipmentChoosen.DistFromAttach), new Azimuth(_equipmentChoosen.AngleFromAttach), Distance.FromCentimeters(userControl.Overlap));
                _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, _equipment, userControl.Headlands);
            }
            else
            {
                try
                {
                    Type equipmentClass = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()).Where(t => String.Equals(t.FullName, _equipmentChoosen.EquipmentClass, StringComparison.Ordinal)).First();
                    if (equipmentClass == null)
                        _equipment = new Harrow(Distance.FromMeters(_equipmentChoosen.WorkWidth), Distance.FromMeters(_equipmentChoosen.DistFromAttach), new Azimuth(_equipmentChoosen.AngleFromAttach), Distance.FromMeters(userControl.Overlap));
                    else if (equipmentClass.IsAssignableFrom(typeof(IEquipment)))
                        _equipment = (IEquipment)Activator.CreateInstance(equipmentClass, Distance.FromMeters(_equipmentChoosen.WorkWidth), Distance.FromMeters(_equipmentChoosen.DistFromAttach), new Azimuth(_equipmentChoosen.AngleFromAttach), Distance.FromMeters(userControl.Overlap));
                    else
                        _equipment = (IEquipment)Activator.CreateInstance(equipmentClass, Distance.FromMeters(_equipmentChoosen.WorkWidth), Distance.FromMeters(_equipmentChoosen.DistFromAttach), new Azimuth(_equipmentChoosen.AngleFromAttach), Distance.FromMeters(userControl.Overlap));

                    if (_equipment is IEquipmentControl)
                    {
                        IEquipmentControl equipmentControl = _equipment as IEquipmentControl;
                        object settings = _config.Sections[equipmentControl.ControllerSettingsType.FullName];
                        if (settings == null)
                            settings = Activator.CreateInstance(equipmentControl.ControllerSettingsType);

                        object controller = equipmentControl.RegisterController(settings);

                        ISettingsCollection settingsCollection = settings as ISettingsCollection;
                        SettingGroup controllerSettings = new SettingGroup(settingsCollection.Name, null, new SettingsCollectionControl(settingsCollection));
                        (controllerSettings.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;

                        SettingGroup settingRoot = _settingsTree.ItemsSource as SettingGroup;
                        ((SettingsCollectionControl)settingRoot.Items[0].SettingControl).Settings.ChildSettings.Add(settingsCollection);
                        settingRoot.Items[0].Items.Add(controllerSettings);
                        _settingsTree.ItemsSource = null;
                        _settingsTree.ItemsSource = settingRoot;

                        if (EQUIPMENTCONTROL_VISUALIZATION.ContainsKey(equipmentControl.ControllerType))
                        {
                            BTN_START_STOP_AUTO.Visibility = Visibility.Visible;
                            BTN_EQUIPMENT.Visibility = Visibility.Visible;
                            while(_equipmentControlGrid.Children.Count > 1)
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
                            BTN_START_STOP_AUTO.Visibility = Visibility.Hidden;
                            BTN_EQUIPMENT.Visibility = Visibility.Hidden;
                            _equipmentGrid.Visibility = Visibility.Hidden;
                            _equipmentStatGrid.Visibility = Visibility.Hidden;
                        }
                    }

                    _farmingMode = (FarmingGPSLib.FarmingModes.IFarmingMode)Activator.CreateInstance(_equipment.FarmingMode, _field, _equipment, userControl.Headlands);
                }
                catch(Exception e)
                {
                    Log.Error("Failed to set euipment and farmingmode", e);
                    return;
                }
            }

            _visualization.SetEquipmentWidth(_equipment.Width);
            

            if (_stateRecovery.ObjectsRecovered.ContainsKey(typeof(FarmingGPSLib.FarmingModes.IFarmingMode)))
            {
                YesNoDialog dialog = new YesNoDialog("Vill du återställa tidigare spårlinjer?");
                if (dialog.ShowDialog().Value)
                    _farmingMode.RestoreObject(_stateRecovery.ObjectsRecovered[typeof(FarmingGPSLib.FarmingModes.IFarmingMode)]);
            }
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadland)
                _visualization.AddLine(line);

            foreach(TrackingLine line in _farmingMode.TrackingLines)
                _visualization.AddLine(line);

            CheckAllTrackingLines();

            YesNoDialog dialogFieldTracker = new YesNoDialog("Vill du radera körd area?");
            if (dialogFieldTracker.ShowDialog().Value)
            {
                if (_fieldTrackerActive)
                    ToggleFieldTracker();
                _fieldTracker.ClearTrack();
            }

            _stateRecovery.AddStateObject(_farmingMode);

            _settingsGrid.Visibility = Visibility.Hidden;
            ShowTrackingLineSettings();
            _farmingMode.FarmingEvent += FarmingEvent;
        }

        private void GetVechileEquipment(GetVechileEquipment userControl)
        {
            _equipmentChoosen = userControl.Equipment;
            if (_receiver != null)
            {
                Position originPosition = new Position( new Latitude(0.0), new Longitude(0.0));
                Position newPosition = originPosition.TranslateTo(new Azimuth(userControl.Vechile.ReceiverAngleFromCenter), Distance.FromMeters(userControl.Vechile.ReceiverDistFromCenter), Ellipsoid.Wgs1984);
                newPosition = newPosition.TranslateTo(new Azimuth(userControl.VechileAttach.AttachAngleFromCenter), Distance.FromMeters(userControl.VechileAttach.AttachDistFromCenter), Ellipsoid.Wgs1984);
                newPosition = newPosition.TranslateTo(new Azimuth(userControl.Equipment.AngleFromAttach), Distance.FromMeters(userControl.Equipment.DistFromAttach), Ellipsoid.Wgs1984);
                _receiver.OffsetDirection = originPosition.BearingTo(newPosition);
                _receiver.OffsetDistance = originPosition.DistanceTo(newPosition);
            }
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
