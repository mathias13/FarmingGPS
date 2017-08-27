using DotSpatial.Positioning;
using DotSpatial.Topology;
using FarmingGPS.Visualization;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.Positioning;
using GpsUtilities;
using GpsUtilities.Reciever;
using FarmingGPS.Camera;
using FarmingGPS.Camera.Axis;
using GpsUtilities.Filter;
using NTRIP;
using NTRIP.Settings;
using SwiftBinaryProtocol;
using SwiftBinaryProtocol.Eventarguments;
using SwiftBinaryProtocol.MessageStructs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
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


namespace FarmingGPS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants

        protected const double MINIMUM_DISTANCE_BETWEEN_POINTS = 0.5;

        protected const double MAXIMUM_DISTANCE_BETWEEN_POINTS = 5.0;

        protected const double MINIMUM_CHANGE_DIRECTION = 3.0;

        protected const double MAXIMUM_CHANGE_DIRECTION = 10.0;

        #endregion;

        #region Private Variables

        private FarmingGPSLib.FieldItems.Field _field;

        private FarmingGPS.Database.DatabaseHandler _database;
        
        private string _cameraIp = String.Empty;

        private MjpegProcessor.MjpegDecoder _decoder;

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

        private FarmingGPSLib.FarmingModes.GeneralHarrowingMode _farmingMode;

        private FarmingGPSLib.Equipment.IEquipment _equipment;

        private bool _ntripConnected = false;

        private Position _trackLinePointA = Position.Empty;
        
        private bool _setTrackLineAB = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.Exit += Current_Exit;
            WindowState = WindowState.Maximized;

            System.Threading.Thread delayedActionsThread = new System.Threading.Thread(new System.Threading.ThreadStart(delayedActions));
            delayedActionsThread.Start();
            this.Loaded += MainWindow_Loaded;
                        
            SqlConnectionStringBuilder connString = new SqlConnectionStringBuilder();
            connString.Encrypt = false;
            connString.TrustServerCertificate = false;
            connString.IntegratedSecurity = false;
            connString.UserID = @"sa";
            connString.Password = "vetinte";
            connString.DataSource = @"192.168.113.1\SQLEXPRESS";
            connString.InitialCatalog = "FarmingDatabase";
            connString.ConnectTimeout = 5;

            //_camera = new AxisCamera("AxisCase");
            _camera = new AxisCamera(System.Net.IPAddress.Parse("192.168.43.132"));
            _camera.CameraConnectedChangedEvent += _camera_CameraConnectedChangedEvent;
            _camera.CameraImageEvent += _camera_CameraImageEvent;
            SetValue(CameraUnavilableProperty, Visibility.Visible);

            _database = new FarmingGPS.Database.DatabaseHandler(connString);
            _getField.AddDatabase(_database);
            _getField.FieldChoosen += _getField_FieldChoosen;

            _distanceTriggerFieldTracker = new DistanceTrigger(MINIMUM_DISTANCE_BETWEEN_POINTS, MAXIMUM_DISTANCE_BETWEEN_POINTS, MINIMUM_CHANGE_DIRECTION, MAXIMUM_CHANGE_DIRECTION);
        }

        #region DependencyProperties

        protected static readonly DependencyProperty FieldTrackerButtonStyleProperty = DependencyProperty.Register("FieldTrackerButtonStyle", typeof(Style), typeof(MainWindow));

        protected static readonly DependencyProperty CameraImageProperty = DependencyProperty.Register("CameraImage", typeof(BitmapImage), typeof(MainWindow));

        protected static readonly DependencyProperty CameraSizeProperty = DependencyProperty.Register("CameraSize", typeof(Style), typeof(MainWindow));

        protected static readonly DependencyProperty CameraUnavilableProperty = DependencyProperty.Register("CameraUnavailable", typeof(Visibility), typeof(MainWindow));

        #endregion

        void Current_Exit(object sender, ExitEventArgs e)
        {
            if (_camera != null)
                if (_camera is IDisposable)
                    (_camera as IDisposable).Dispose();
            _ntripClient.Dispose();
            _receiver.Dispose();
            if(_sbpReceiverSender != null)
            _sbpReceiverSender.Dispose();
            _database.Dispose();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {   
            SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PLAY"));
            SetValue(CameraSizeProperty, (Style)this.FindResource("PiPvideo"));
        }
        
        void delayedActions()
        {
            System.Threading.Thread.Sleep(1000);
            FarmingGPSLib.Equipment.Harrow harrow = new FarmingGPSLib.Equipment.Harrow(Distance.FromMeters(24.0), Distance.FromMeters(0.0), new Azimuth(180), Distance.FromCentimeters(0));
            _equipment = harrow;

            _visualization.SetEquipmentWidth(harrow.Width);

            _speedBar.Unit = SpeedUnit.KilometersPerHour;
            _speedBar.SetSpeed(Speed.FromKilometersPerHour(2.4));
            _workedAreaBar.Unit = AreaUnit.SquareKilometers;
            _fieldTracker.AreaChanged += _fieldTracker_AreaChanged;
            _receiver_FixQualityUpdate(this, FixQuality.FixedRealTimeKinematic);
            _visualization.AddFieldTracker(_fieldTracker);

            //_sbpReceiverSender = new SBPReceiverSender(System.Net.IPAddress.Parse("192.168.0.222"), 55555);
            _sbpReceiverSender = new SBPReceiverSender("COM4", 115200, false);
            //_receiver = new Piksi(_sbpReceiverSender, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(1000));
            //_receiver.MinimumSpeedLockHeading = Speed.FromKilometersPerHour(1.0);
            _receiver = new KeyboardSimulator(this, new Position3D(Distance.FromMeters(0.0), new Longitude(13.8548025), new Latitude(58.5104914)), false);
            _receiver.BearingUpdate += _receiver_BearingUpdate;
            _receiver.PositionUpdate += _receiver_PositionUpdate;
            _receiver.SpeedUpdate += _receiver_SpeedUpdate;
            _receiver.FixQualityUpdate += _receiver_FixQualityUpdate;
            _speedBar.Unit = SpeedUnit.KilometersPerHour;

            ClientSettings clientSettings = new ClientSettings();
            clientSettings.IPorHost = "nolgarden.net";
            clientSettings.PortNumber = 5000;
            clientSettings.NTRIPMountPoint = "NolgardenSBP";
            clientSettings.NTRIPUser = new NTRIP.Settings.NTRIPUser("Mathias", "vetinte");
            _ntripClient = new NTRIP.ClientService(clientSettings);
            _ntripClient.StreamDataReceivedEvent += _ntripClient_StreamDataReceivedEvent;
            _ntripClient.ConnectionExceptionEvent += _ntripClient_ConnectionExceptionEvent;
            _ntripClient.Connect();
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
                SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PAUSE"));
            else
                SetValue(FieldTrackerButtonStyleProperty, (Style)this.FindResource("BUTTON_PLAY"));
        }

        #endregion

        #region Field Events

        private void _getField_FieldChoosen(object sender, List<Position> e)
        {
            _field = new Field(e, DotSpatial.Projections.KnownCoordinateSystems.Projected.UtmWgs1984.WGS1984UTMZone33N);
            _fieldTracker.FieldToCalculateAreaWithin = _field;
            _workedAreaBar.SetField(_field);
            _visualization.AddField(_field);

            _farmingMode = new FarmingGPSLib.FarmingModes.GeneralHarrowingMode(_field, _equipment, 1);
            foreach (TrackingLine line in _farmingMode.TrackingLinesHeadLand)
                _visualization.AddLine(line);

            _settingsGrid.Visibility = Visibility.Hidden;
            ShowTrackingLineSettings();
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
                if (_fieldTracker.GetTrackingLineCoverage(line) > 0.9)
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

        #region NTRIP Events

        void _ntripClient_StreamDataReceivedEvent(object sender, NTRIP.Eventarguments.StreamReceivedArgs e)
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
                if (_ntripConnected)
                    _NTRIPState.Background = Brushes.Green;
                else
                    _NTRIPState.Background = Brushes.Red;
            }
            else
                Dispatcher.Invoke(new Action(ChangeNTRIPState), System.Windows.Threading.DispatcherPriority.Render);
        }

        #endregion

        #region Receiver Events

        private void _receiver_FixQualityUpdate(object sender, FixQuality fixQuality)
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
                Dispatcher.Invoke(new Action<object, FixQuality>(_receiver_FixQualityUpdate), System.Windows.Threading.DispatcherPriority.Render, this, fixQuality);
        }

        private void _receiver_PositionUpdate(object sender, Position actualPosition)
        {
            IReceiver receiver = sender as IReceiver;
            if (_field == null)
                return;
            Coordinate actualCoordinate = _field.GetPositionInField(_equipment.GetCenter(actualPosition, receiver.CurrentBearing));
            Azimuth actualHeading = receiver.CurrentBearing;
            _visualization.UpdatePosition(actualCoordinate, actualHeading);
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

        #endregion
    }
}
