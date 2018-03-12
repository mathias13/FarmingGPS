using DotSpatial.Positioning;
using DotSpatial.Topology;
using FarmingGPS.Camera;
using FarmingGPS.Camera.Axis;
using FarmingGPS.Dialogs;
using FarmingGPS.Settings;
using FarmingGPS.Settings.Database;
using FarmingGPS.Settings.NTRIP;
using FarmingGPS.Settings.Receiver;
using FarmingGPS.Usercontrols;
using FarmingGPS.Visualization;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
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
using System.Windows;
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

        #endregion;

        #region Private Variables

        Configuration _config;

        private Field _field;

        private Database.DatabaseHandler _database;
        
        private string _cameraIp = String.Empty;
        
        private ClientService _ntripClient;

        private SBPReceiverSender _sbpReceiverSender;

        private IReceiver _receiver;

        private ICamera _camera;
                
        private DateTime _trackingLineEvaluationTimeout = DateTime.MinValue;

        private TrackingLine _activeTrackingLine = null;
        
        private FieldTracker _fieldTracker = new FieldTracker();

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

            this.Loaded += MainWindow_Loaded;

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
            if (_equipment != null)
                if (_equipment is IDisposable)
                    (_equipment as IDisposable).Dispose();
            if (_camera != null)
                if (_camera is IDisposable)
                    (_camera as IDisposable).Dispose();
            if(_ntripClient != null)
                _ntripClient.Dispose();
            if (_receiver != null)
                if (_receiver is IDisposable)
                    (_receiver as IDisposable).Dispose();
            if(_sbpReceiverSender != null)
                _sbpReceiverSender.Dispose();
            if(_database != null)
                _database.Dispose();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {   
            SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PLAY"));
            SetValue(CameraSizeProperty, (Style)this.FindResource("PiPvideo"));

            SetupSettingsPanel(null);
            
            _speedBar.Unit = SpeedUnit.KilometersPerHour;
            _speedBar.SetSpeed(Speed.FromKilometersPerHour(0.0));
            _workedAreaBar.Unit = AreaUnit.SquareKilometers;
            _fieldTracker.AreaChanged += _fieldTracker_AreaChanged;
            _visualization.AddFieldTracker(_fieldTracker);

#if DEBUG
            _receiver = new KeyboardSimulator(this, new Position3D(Distance.FromMeters(0.0), new Longitude(13.8531152), new Latitude(58.50953)), false);
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
            List<Position> positions = new List<Position>();
            positions.Add(new Position(new Latitude(58.51114), new Longitude(13.8537299)));
            positions.Add(new Position(new Latitude(58.509705), new Longitude(13.8532929)));
            positions.Add(new Position(new Latitude(58.509073), new Longitude(13.8606883)));
            positions.Add(new Position(new Latitude(58.510477), new Longitude(13.8611945)));
            positions.Add(new Position(new Latitude(58.51114), new Longitude(13.8537299)));
            FieldChoosen(positions);
            
            FarmingGPSLib.Equipment.BogBalle.L2Plus fertilizer = new FarmingGPSLib.Equipment.BogBalle.L2Plus(Distance.FromMeters(5.0), Distance.FromMeters(0.0), new Azimuth(180), new FarmingGPSLib.Equipment.BogBalle.Calibrator("COM1", 1000));
            _equipment = fertilizer;
            _visualization.SetEquipmentWidth(fertilizer.Width);
            _farmingMode = new FarmingGPSLib.FarmingModes.FertilizingMode(_field, _equipment, 1);
            _farmingMode.FarmingEvent += FarmingEvent;
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
                _visualization.AddLine(line);
            ShowTrackingLineSettings();

            _equipmentControlGrid.Children.Add(new FarmingGPS.Usercontrols.Equipments.BogballeCalibrator(new FarmingGPSLib.Equipment.BogBalle.Calibrator("COM1", 1000)));
#endif

        }

        private void _sbpReceiverSender_ReadExceptionEvent(object sender, SBPReadExceptionEventArgs e)
        {
            Log.Error("SBPReaderException", e.Exception);
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
                    {
                        equipmentControl.Start();
                        if (!_fieldTrackerActive)
                            Dispatcher.Invoke(new Action(ToggleFieldTracker), DispatcherPriority.Normal);
                    }
                    else if (e.Contains("STOP"))
                    {
                        equipmentControl.Start();
                        if (_fieldTrackerActive)
                            Dispatcher.Invoke(new Action(ToggleFieldTracker), DispatcherPriority.Normal);
                    }
                }
            }
        }
        
        #endregion

        #region Field Events

        private void FieldChoosen(List<Position> e)
        {
            _field = new Field(e, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            _fieldTracker.FieldToCalculateAreaWithin = _field;
            _workedAreaBar.SetField(_field);
            _visualization.AddField(_field);
            
            _settingsGrid.Visibility = Visibility.Hidden;
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
            _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, _equipment, 1);
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
            {
                _visualization.AddLine(line);
                if (_fieldTracker.GetTrackingLineCoverage(line) > 0.97)
                    line.Depleted = true;
            }
            ShowTrackingLineSettings();
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
            _receiver.MinimumSpeedLockHeading = Speed.FromKilometersPerHour(1.0);
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
            _farmingMode.UpdateEvents(actualCoordinate, actualHeading);
            if (_equipment != null)
            {
                Coordinate leftTip = _field.GetPositionInField(_equipment.GetLeftTip(actualPosition, actualHeading));
                Coordinate rightTip = _field.GetPositionInField(_equipment.GetRightTip(actualPosition, actualHeading));

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
                _activeTrackingLine.Active = true;
                OrientationToLine orientationToLine = _activeTrackingLine.GetOrientationToLine(actualCoordinate, actualHeading);
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
                if (_selectedTrackingLine >= _farmingMode.TrackingLinesHeadLand.Count)
                    _selectedTrackingLine = 0;
                _visualization.FocusTrackingLine(_farmingMode.TrackingLinesHeadLand[_selectedTrackingLine]);
            }
            else
        {
                _selectedTrackingLine = 0;
                _visualization.FocusTrackingLine(_farmingMode.TrackingLinesHeadLand[_selectedTrackingLine]);
                BTN_CHOOSE_TRACKLINE.Style = (Style)this.FindResource("BUTTON_MOVE_NEXT");
                BTN_CONFIRM_TRACKLINE.Visibility = Visibility.Visible;
            }
        }

        private void BTN_CONFIRM_TRACKLINE_Click(object sender, RoutedEventArgs e)
        {
            BTN_CHOOSE_TRACKLINE.Style = (Style)this.FindResource("BUTTON_CHOOSE_TRACKLINE");
            BTN_CONFIRM_TRACKLINE.Visibility = Visibility.Collapsed;
            _trackLineGrid.Visibility = Visibility.Collapsed;
            _farmingMode.CreateTrackingLines(_farmingMode.TrackingLinesHeadLand[_selectedTrackingLine]);
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
            if (_equipmentControlGrid.Visibility == System.Windows.Visibility.Hidden)
                _equipmentControlGrid.Visibility = System.Windows.Visibility.Visible;
            else
                _equipmentControlGrid.Visibility = System.Windows.Visibility.Hidden;
        }

        private void BTN_START_STOP_AUTO_Checked(object sender, RoutedEventArgs e)
        {
            _startStopAuto = BTN_START_STOP_AUTO.IsChecked.Value;
        }

        #endregion

        #region Settings

        private void SetupSettingsPanel(SettingsCollection settings)
        {
            ClientSettingsExt sectionNTRIP = (ClientSettingsExt)_config.Sections["NTRIP"];
            ISettingsCollection ntripClient;
            if (sectionNTRIP != null)
            {
                ntripClient = new ClientSettingsExt(sectionNTRIP);
                SetupNTRIP(sectionNTRIP);
            }
            else
                ntripClient = new ClientSettingsExt();

            DatabaseConn sectionDatabase = (DatabaseConn)_config.Sections["Databas"];
            ISettingsCollection database;
            if (sectionDatabase != null)
            {
                database = new DatabaseConn(sectionDatabase);
                SetupDatabase(sectionDatabase);
            }
            else
                database = new DatabaseConn();
            
            SBPSerial sectionReceiver = (SBPSerial)_config.Sections["SBPSeriell"];
            ISettingsCollection receiver;
            if (sectionReceiver != null)
            {
                receiver = new SBPSerial(sectionReceiver);
                SetupReceiver(sectionReceiver);
            }
            else
                receiver = new SBPSerial();

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
            SettingGroup redskap = new SettingGroup("Redskap", new SettingGroup[] { farmingMode }, new GetVechileEquipment());
            (redskap.SettingControl as ISettingsChanged).SettingChanged += SettingItem_SettingChanged;
            SettingGroup settingRoot = new SettingGroup("Inställningar", new SettingGroup[] { connectionGroup, field, redskap }, null);
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
            else if (sender is SettingsCollectionControl)
            {
                SettingsCollectionControl settingControl = sender as SettingsCollectionControl;
                if (settingControl.Settings is ConfigurationSection)
                {
                    ConfigurationSection configSection = _config.Sections[settingControl.Settings.Name];
                    if (configSection == null)
                        _config.Sections.Add(settingControl.Settings.Name, settingControl.Settings as ConfigurationSection);

                    _config.Save(ConfigurationSaveMode.Minimal, true);
                }
                if (settingControl.Settings is DatabaseConn)
                    SetupDatabase(settingControl.Settings as DatabaseConn);
                else if (settingControl.Settings is ClientSettingsExt)
                    SetupNTRIP(settingControl.Settings as ClientSettings);
                else if (settingControl.Settings is SBPSerial)
                    SetupReceiver(settingControl.Settings as SBPSerial);
            }
        }

        private void SetEquipmentAndFarmingMode(FarmingMode userControl)
        {
            if (_equipmentChoosen == null)
                return;

            if (_farmingMode != null)
            {
                foreach (TrackingLine trackingLine in _farmingMode.TrackingLines)
                    _visualization.DeleteLine(trackingLine);
                foreach (TrackingLine trackingLine in _farmingMode.TrackingLinesHeadLand)
                    _visualization.DeleteLine(trackingLine);
            }

            //TODO make this better
            if (_equipmentChoosen.EquipmentId == 1)
            {
                FarmingGPSLib.Equipment.BogBalle.Calibrator calibrator = new FarmingGPSLib.Equipment.BogBalle.Calibrator("COM5", 1000);
                FarmingGPSLib.Equipment.BogBalle.L2Plus fertilizer = new FarmingGPSLib.Equipment.BogBalle.L2Plus(Distance.FromMeters(_equipmentChoosen.WorkWidth), 
                    Distance.FromMeters(_equipmentChoosen.DistFromAttach),
                    new Azimuth(_equipmentChoosen.AngleFromAttach),
                    Distance.FromCentimeters(userControl.Overlap),
                    calibrator);
                _equipment = fertilizer;
                _farmingMode = new FarmingGPSLib.FarmingModes.FertilizingMode(_field, _equipment, 1);
                _farmingMode.FarmingEvent += FarmingEvent;
                _equipmentControlGrid.Children.Add(new FarmingGPS.Usercontrols.Equipments.BogballeCalibrator(calibrator));
            }
            else
            {
                Harrow harrow = new Harrow(Distance.FromMeters(_equipmentChoosen.WorkWidth), Distance.FromMeters(_equipmentChoosen.DistFromAttach), new Azimuth(_equipmentChoosen.AngleFromAttach), Distance.FromCentimeters(userControl.Overlap));
                _equipment = harrow;
                _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, _equipment, userControl.Headlands);
            }

            _visualization.SetEquipmentWidth(_equipment.Width);
            

            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
                _visualization.AddLine(line);

            if (_fieldTrackerActive)
                ToggleFieldTracker();
            _fieldTracker.ClearTrack();

            _settingsGrid.Visibility = Visibility.Hidden;
            ShowTrackingLineSettings();
        }

        private void GetVechileEquipment(GetVechileEquipment userControl)
        {
            _equipmentChoosen = userControl.Equipment;
            if (_receiver != null)
            {
                Position originPosition = new Position( new Latitude(0.0), new Longitude(0.0));
                Position newPosition = originPosition.TranslateTo(new Azimuth(userControl.Vechile.ReceiverAngleFromCenter), Distance.FromMeters(userControl.Vechile.ReceiverDistFromCenter), Ellipsoid.Wgs1984);
                newPosition = newPosition.TranslateTo(new Azimuth(userControl.VechileAttach.AttachAngleFromCenter), Distance.FromMeters(userControl.Vechile.ReceiverDistFromCenter), Ellipsoid.Wgs1984);
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
