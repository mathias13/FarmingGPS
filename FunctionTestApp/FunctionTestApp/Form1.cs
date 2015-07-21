using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotSpatial.Positioning;
using GpsCorrectionClient;
using GpsCorrectionServiceProtocol;
using GpsCorrectionServiceProtocol.ClientMessageTypes;
using GpsCorrectionServiceProtocol.ServerMessageTypes;
using GpsUtilities;
using SwiftBinaryProtocol;
using SwiftBinaryProtocol.MessageStructs;
using SwiftBinaryProtocol.Eventarguments;

namespace FunctionTestApp
{
    public partial class TestAppForm : Form
    {
        private NmeaInterpreter _interpreter = new NmeaInterpreter();
                                   
        private ServiceConnector _correctionService;

        private Position _actualPositionBaseline;

        private Position _actualPosition;

        private Position? _pointActRef;
                                           
        private int _lastSecond = 0;

        private int _positionCount = 0;

        private GPS_CORRECTION_INFO? _lastCorrection;

        private bool stopGpsFinder = false;

        private object _syncObject = new object();

        private System.Threading.Thread gpsThread;
        
        private Azimuth _corrBearing;

        private Distance _corrDist;

        private SBPReceiverSender _sbpReceiverSender;

        private SBPRawReceiverSender _sbpRawReceiverSender;

        private CartesianPoint _knownPosition;

        private Segment _line1;

        private Segment _line2;

        private Segment _line3;

        private BaselineECEF _baseLineECEF;

        private PositionLLH _positionLLH;

        private BaselineNED _baseLineNED;

        private IARState _iarState;

        private NTRIP.ClientService _ntripClient;

        private DateTime _nextInvoke = DateTime.MinValue;

        private Queue<string> _messageQueue = new Queue<string>();

        private Queue<ThreadState> _threadQueue = new Queue<ThreadState>();

        private int _observationCount = 0;

        private double _lastObservationTimeOfWeek = double.MinValue;

        private ushort _lastObservationWeekNumber = ushort.MinValue;
        
        private UARTState? _uartState = null;

        private bool _activateNtrip = false;

        private DateTime _gpsTime = DateTime.MinValue;

        private int _hypothesis = 0;

        private CsvFileWriter _posWriter;

        private CsvFileWriter _baselineWriter;

        private CsvFileWriter _velocityWriter;

        private StreamWriter _logWriter;

        private DateTime _nextFlush = DateTime.Now.AddSeconds(5.0);

        private List<ObservationHeader> _observationsBase = new List<ObservationHeader>();

        private List<ObservationHeader> _observationsRover = new List<ObservationHeader>();

        public TestAppForm()
        {
            InitializeComponent();

            Application.ApplicationExit += Application_ApplicationExit;
            string dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _posWriter = new CsvFileWriter(@"C:\swift\logs\position_log_" + dateTime + ".csv");
            CsvRow row = new CsvRow();
            row.Add("time");
            row.Add("latitude(degrees)");
            row.Add("longitude(degrees)");
            row.Add("altitude(meters)");
            row.Add("n_sats");
            row.Add("flags");
            _posWriter.WriteRow(row);
            _baselineWriter = new CsvFileWriter(@"C:\swift\logs\baseline_log_" + dateTime + ".csv");
            row = new CsvRow();
            row.Add("time");
            row.Add("north(meters)");
            row.Add("east(meters)");
            row.Add("down(meters)");
            row.Add("distance(meters)");
            row.Add("num_sats");
            row.Add("flags");
            row.Add("num_hypothesis");
            _baselineWriter.WriteRow(row);
            _velocityWriter = new CsvFileWriter(@"C:\swift\logs\velocity_log_" + dateTime + ".csv");
            row = new CsvRow();
            row.Add("time");
            row.Add("north(m/s)");
            row.Add("east(m/s)");
            row.Add("down(m/s)");
            row.Add("speed(m/s)");
            row.Add("num_sats");
            _velocityWriter.WriteRow(row);
            _logWriter = new CsvFileWriter(@"C:\swift\logs\log_" + dateTime + ".txt");

            _interpreter.IsFilterEnabled = false;
            _interpreter.ExceptionOccurred += _interpreter_ExceptionOccurred;

            //_receiverManager = new GPSReceiverManager(TimeSpan.FromSeconds(0.5));
            //_receiverManager.PositionUpdateEvent += _receiverManager_PositionUpdateEvent;

            //gpsThread = new System.Threading.Thread(new System.Threading.ThreadStart(gpsFinder));
            //gpsThread.Start();

            _knownPosition = new Position3D(new Distance(84.0, DistanceUnit.Meters), new Latitude(58.512115), new Longitude(13.85485)).ToCartesianPoint();
            //_knownPosition = new Position3D(new Distance(100.0, DistanceUnit.Meters), new Latitude(37.43), new Longitude(-122.17)).ToCartesianPoint();


            NTRIP.Settings.ClientSettings clientSettings = new NTRIP.Settings.ClientSettings();
            clientSettings.IPorHost = "nolgarden.net";
            //clientSettings.IPorHost = "192.168.0.50";
            clientSettings.PortNumber = 5000;
            clientSettings.NTRIPMountPoint = "ExampleName";
            clientSettings.NTRIPUser = new NTRIP.Settings.NTRIPUser("ExAmPlEuSeR", "ExAmPlEpAsSwOrD");
            _ntripClient = new NTRIP.ClientService(clientSettings);
            _ntripClient.ConnectionExceptionEvent += _ntripClient_ConnectionExceptionEvent;
            _ntripClient.SourceTableReceivedEvent += _ntripClient_SourceTableReceivedEvent;
            _ntripClient.StreamDataReceivedEvent += _ntripClient_StreamDataReceivedEvent;
            //_ntripClient.RTCMReceivedEvent += _ntripClient_RTCMReceivedEvent;
            //_ntripClient.SBPRawReceivedEvent += _ntripClient_SBPRawReceivedEvent;
            _ntripClient.Connect();

            _sbpReceiverSender = new SBPReceiverSender("COM6", 1000000);
            _sbpReceiverSender.ReceivedMessageEvent += _sbpReceiverSender_ReceivedMessageEvent;
            _sbpReceiverSender.ReadExceptionEvent += _sbpReceiverSender_ReadExceptionEvent;

            //_sbpRawReceiverSender = new SBPRawReceiverSender("COM4", 1000000);
            //_sbpRawReceiverSender.ReceivedRawMessageEvent += _sbpRawReceiverSender_ReceivedRawMessageEvent;
            //_sbpRawReceiverSender.ReadExceptionEvent += _sbpReceiverSender_ReadExceptionEvent;
        }

        void _ntripClient_StreamDataReceivedEvent(object sender, NTRIP.Eventarguments.StreamReceivedArgs e)
        {
            if(_activateNtrip)
                _sbpReceiverSender.SendMessage(e.DataStream);
        }

        void _ntripClient_SourceTableReceivedEvent(object sender, NTRIP.Eventarguments.SourceTableArgs e)
        {
            foreach (NTRIP.MountPoint mountPoint in e.MountPoints)
                ;

            _ntripClient.Disconnect();

            _ntripClient.Connect(e.MountPoints[0].Name);
        }

        void _sbpRawReceiverSender_ReceivedRawMessageEvent(object sender, SBPRawMessageEventArgs e)
        {
            switch (e.MessageType)
            {
                case SBP_Enums.MessageTypes.OBS_HDR:                    
                    lock (_syncObject)
                        _messageQueue.Enqueue("Observation message received " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                    break;
            }
            if (_nextInvoke < DateTime.Now)
            {
                BeginInvoke(new MethodInvoker(UpdateFixLabel));
                BeginInvoke(new MethodInvoker(UpdateLabels));
                BeginInvoke(new MethodInvoker(UpdateDebugListbox));
                _nextInvoke = DateTime.Now.AddSeconds(0.5);
            }
        }

        //void _ntripClient_SBPRawReceivedEvent(object sender, NTRIP.Eventarguments.SBPRawReceived e)
        //{
        //    _sbpReceiverSender.SendMessage(e.Message);
        //    //List<byte> bytes = new List<byte>();
        //    //bytes.AddRange(e.Message);
        //    //bytes.RemoveRange(0, 6);
        //    //bytes.RemoveAt(bytes.Count - 1);
        //    //bytes.RemoveAt(bytes.Count - 1);
        //    //ObservationHeader observationHeader = new ObservationHeader(bytes.ToArray());
        //    //if (observationHeader.Count == 0)
        //    //{
        //    //    _lastObservationTimeOfWeek = observationHeader.TimeOfWeek;
        //    //    _lastObservationWeekNumber = observationHeader.WeekNumber;
        //    //    _observationCount = observationHeader.Count;
        //    //}
        //    //else if (observationHeader.Count != (_observationCount + 1) || observationHeader.TimeOfWeek != _lastObservationTimeOfWeek || observationHeader.WeekNumber != _lastObservationWeekNumber)
        //    //{
        //    //    lock (_syncObject)
        //    //    {
        //    //        _messageQueue.Enqueue("Dropped observation message\n");
        //    //        _messageQueue.Enqueue("_observationCount: " + _observationCount.ToString() + "\n");
        //    //        _messageQueue.Enqueue("observationHeader.Count: " + observationHeader.Count.ToString() + "\n");
        //    //        _messageQueue.Enqueue("observationHeader.TimeOfWeek: " + observationHeader.TimeOfWeek.ToString() + "\n");
        //    //        _messageQueue.Enqueue("_lastObservationTimeOfWeek: " + _lastObservationTimeOfWeek.ToString() + "\n");
        //    //        _messageQueue.Enqueue("observationHeader.WeekNumber: " + observationHeader.WeekNumber.ToString() + "\n");
        //    //        _messageQueue.Enqueue("_lastObservationWeekNumber: " + _lastObservationWeekNumber.ToString() + "\n");
        //    //    }
        //    //    _observationCount = 0;
        //    //}
        //    //else if (observationHeader.Count + 1 == observationHeader.Total)
        //    //{
        //    //    TimeSpan diff = DateTime.Now.AddHours(-1.0) - observationHeader.GPSDateTime;
        //    //    lock (_syncObject)
        //    //    {
        //    //        _messageQueue.Enqueue("Complete observation message received " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        //    //        _messageQueue.Enqueue("Difference GpsTime: " + diff.ToString(@"hh\:mm\:ss\.ff") + "\n");
        //    //    }
        //    //}
        //}

        //void _ntripClient_RTCMReceivedEvent(object sender, NTRIP.Eventarguments.RTCMReceived e)
        //{
        //    if (e.MsgType == RTCM.MESSAGES.MessageEnum.Messages.MSG_1002)
        //    {
        //        RTCM.MESSAGES.Message_1002 message = (RTCM.MESSAGES.Message_1002)e.Message;
        //        ;
        //    }
        //}

        void _ntripClient_ConnectionExceptionEvent(object sender, NTRIP.Eventarguments.ConnectionExceptionArgs e)
        {
            if (e.Exception != null)
                ;
        }

        void _sbpReceiverSender_ReadExceptionEvent(object sender, SBPReadExceptionEventArgs e)
        {
            lock (_syncObject)
                _messageQueue.Enqueue(e.Exception.Message + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            if (_nextInvoke < DateTime.Now)
            {
                BeginInvoke(new MethodInvoker(UpdateFixLabel));
                BeginInvoke(new MethodInvoker(UpdateLabels));
                BeginInvoke(new MethodInvoker(UpdateDebugListbox));
                _nextInvoke = DateTime.Now.AddSeconds(0.5);
            }
        }

        void _sbpReceiverSender_ReceivedMessageEvent(object sender, SBPMessageEventArgs e)
        {
            switch(e.MessageType)
            {
                case SBP_Enums.MessageTypes.ACQ_RESULT:
                    AquisitionResult aquisitionResult = (AquisitionResult)e.Data;
                    break;

                case SBP_Enums.MessageTypes.EPHEMERIS:
                    Ephemeris ephemeris = (Ephemeris)e.Data;
                    break;

                case SBP_Enums.MessageTypes.TRACKING_STATE:
                    TrackingState trackingState = (TrackingState)e.Data;
                    break;

                case SBP_Enums.MessageTypes.HEARTBEAT:
                    Heartbeat heartbeat = (Heartbeat)e.Data;
                    break;

                case SBP_Enums.MessageTypes.GPSTIME:
                    GPSTime gpsTime = (GPSTime)e.Data;
                    _gpsTime = gpsTime.GPSDateTime;
                    break;

                case SBP_Enums.MessageTypes.BASELINE_ECEF:
                    BaselineECEF baseLineECEF = (BaselineECEF)e.Data;
                    CartesianPoint baseLine = new CartesianPoint(new Distance(baseLineECEF.BaselineXMeters, DistanceUnit.Meters),
                                                                new Distance(baseLineECEF.BaselineYMeters, DistanceUnit.Meters),
                                                                new Distance(baseLineECEF.BaselineZMeters, DistanceUnit.Meters));
                    Position3D position = (_knownPosition + baseLine).ToPosition3D();
                    lock(_syncObject)
                    { 
                        _actualPositionBaseline = new Position(position.Longitude, position.Latitude);
                        _baseLineECEF = baseLineECEF;
                    }
                    break;

                case SBP_Enums.MessageTypes.BASELINE_NED:
                    BaselineNED baseLineNED = (BaselineNED)e.Data;
                    LogBaseline(baseLineNED);
                    lock (_syncObject)
                        _baseLineNED = baseLineNED;
                    break;

                case SBP_Enums.MessageTypes.POS_LLH:
                    PositionLLH positionLLH = (PositionLLH)e.Data;
                    LogPosition(positionLLH);
                    _actualPosition = new Position(new Longitude(positionLLH.PosLongitude), new Latitude(positionLLH.PosLatitude));
                    lock (_syncObject)
                        _positionLLH = positionLLH;
                    BeginInvoke(new MethodInvoker(UpdateLabels));
                    break;

                case SBP_Enums.MessageTypes.VEL_NED:
                    VelocityNED velocityNED = (VelocityNED)e.Data;
                    LogVelocity(velocityNED);
                    break;

                case SBP_Enums.MessageTypes.POS_ECEF:
                    PosistionECEF positionECEF = (PosistionECEF)e.Data;
                    break;

                case SBP_Enums.MessageTypes.OBS_HDR:
                    ObservationHeader observationHeader = (ObservationHeader)e.Data;
                    if(e.SenderID == 0)
                    {
                        if (_observationsBase.Count != observationHeader.Count)
                            _observationsBase.Clear();
                        else
                        {
                            _observationsBase.Add(observationHeader);
                            if(_observationsBase.Count == observationHeader.Total)
                            {
                                Invoke(new MethodInvoker(UpdateBaseObservations));
                                _observationsBase.Clear();
                            }
                        }
                    }
                    else
                    {
                        if (_observationsRover.Count != observationHeader.Count)
                            _observationsRover.Clear();
                        else
                        {
                            _observationsRover.Add(observationHeader);
                            if (_observationsRover.Count == observationHeader.Total)
                            {
                                Invoke(new MethodInvoker(UpdateRoverObservations));
                                _observationsRover.Clear();
                            }
                        }
                    }
                    break;

                case SBP_Enums.MessageTypes.IAR_STATE:
                    _iarState = (IARState)e.Data;
                    _hypothesis = (int)_iarState.NumberOfHypothesis;
                    BeginInvoke(new MethodInvoker(UpdateFixLabel));
                    break;

                case SBP_Enums.MessageTypes.PRINT:
                    Print print = (Print)e.Data;
                    Log(print);
                    lock (_syncObject)
                        _messageQueue.Enqueue(print.Message);
                    break;

                case SBP_Enums.MessageTypes.THREAD_STATE:
                    ThreadState threadState = (ThreadState)e.Data;
                    lock (_syncObject)
                        _threadQueue.Enqueue(threadState);
                    break;

                case SBP_Enums.MessageTypes.UART_STATE:
                    UARTState uartState = (UARTState)e.Data;
                    lock (_syncObject)
                        _uartState = uartState;
                    break;
            }
            if (_nextInvoke < DateTime.Now)
            {
                BeginInvoke(new MethodInvoker(UpdateFixLabel));
                BeginInvoke(new MethodInvoker(UpdateLabels));
                BeginInvoke(new MethodInvoker(UpdateDebugListbox));
                BeginInvoke(new MethodInvoker(UpdateCpuGrid));
                _nextInvoke = DateTime.Now.AddSeconds(0.5);
            }
        }

        void _interpreter_ExceptionOccurred(object sender, ExceptionEventArgs e)
        {
            Utilities.Log.Log.Error(e.Exception);
        }

        void gpsFinder()
        {
            bool newSearch = false;
            while (!stopGpsFinder)
            {
                if(!_interpreter.IsRunning)
                {
                    if (!Devices.IsDetectionInProgress && !newSearch)
                    {
                        newSearch = true;
                        Devices.BeginDetection();
                    }
                    else if (!Devices.IsDetectionInProgress && newSearch)
                    {
                        newSearch = false;
                        if (Devices.GpsDevices.Count > 0)
                            _interpreter.Start();
                    }
                    //Devices.BeginDetection();
                    //Devices.WaitForDetection(TimeSpan.FromMinutes(1.0));
                    //if (Devices.GpsDevices.Count > 1)
                    //{
                    //    Distance[] distances1 = new Distance[1]{Distance.FromMeters(1)};
                    //    Azimuth[] bearings1 = new Azimuth[1]{new Azimuth(-90)};
                    //    Distance[] distances2 = new Distance[1]{Distance.FromMeters(1)};
                    //    Azimuth[] bearings2 = new Azimuth[1]{new Azimuth(90)};
                    //    Reposition repos1 = new Reposition(distances1, bearings1);
                    //    Reposition repos2 = new Reposition(distances2, bearings2);
                    //    KeyValuePair<Device, Reposition>? device1 = null;
                    //    KeyValuePair<Device, Reposition>? device2 = null;

                    //    if(Devices.GpsDevices[0].Name == "BT-GPS-39042C")
                    //        device1 = new KeyValuePair<Device,Reposition>(Devices.GpsDevices[0], repos1);
                    //    else if (Devices.GpsDevices[1].Name == "BT-GPS-39042C")
                    //        device1 = new KeyValuePair<Device, Reposition>(Devices.GpsDevices[1], repos1);


                    //    if (Devices.GpsDevices[0].Name == "BT-GPS-38FCEF")
                    //        device2 = new KeyValuePair<Device, Reposition>(Devices.GpsDevices[0], repos2);
                    //    else if (Devices.GpsDevices[1].Name == "BT-GPS-38FCEF")
                    //        device2 = new KeyValuePair<Device, Reposition>(Devices.GpsDevices[1], repos2);

                    //    _receiverManager.Start(device1, device2);

                    //    return;
                    //}
                }
                System.Threading.Thread.Sleep(100);
            }

        }
        
        void Application_ApplicationExit(object sender, EventArgs e)
        {
            if(_sbpReceiverSender != null)
                _sbpReceiverSender.Dispose();


            //_interpreter.PositionChanged -= _interpreter_PositionChanged;
            //_interpreter.FixQualityChanged -= _interpreter_FixQualityChanged;
            stopGpsFinder = true;
            //gpsThread.Join();
            //_correctionService.ServerConnectedChangedEvent -= _correctionService_ServerConnectedChangedEvent;
            //_correctionService.ServerMessageEvent -= _correctionService_ServerMessageEvent;
            //_correctionService.SendServerMessage(new ClientMessage(MessageTypes.ClientMessageTypes.GOODBYE, null));
            //_correctionService.Dispose();
            //_interpreter.Stop();
            if (_interpreter.Device != null)
            {
                _interpreter.Device.Close();
                _interpreter.Device.Dispose();
            }

            if(_ntripClient != null)
                _ntripClient.Dispose();

            _posWriter.Flush();
            _baselineWriter.Flush();
            _velocityWriter.Flush();
            _logWriter.Flush();
        }

        void _correctionService_ServerMessageEvent(object sender, ServerMessageEventArgs e)
        {
            lock (_syncObject)
            {
                if (e.Message.Type == MessageTypes.ServerMessageTypes.GPS_CORRECTION)
                {
                    _lastCorrection = (GPS_CORRECTION_INFO)e.Message.MessageData;
                    BeginInvoke(new MethodInvoker(UpdateCorrectionDistance));
                }
                else if (e.Message.Type == MessageTypes.ServerMessageTypes.GPS_QUALITY)
                    ;
            }
        }

        protected void UpdateCorrectionDistance()
        {
            lock (_syncObject)
            {
                Position corrected = new Position(new Latitude(_lastCorrection.Value.LatitudeActual), new Longitude(_lastCorrection.Value.LongitudeActual));
                Position known = new Position(new Latitude(_lastCorrection.Value.LatitudeKnown), new Longitude(_lastCorrection.Value.LongitudeKnown));
                lblCorrectionDist.Text = known.DistanceTo(corrected).ToMeters().ToString("0.00 m");
                lblCorrectionDist.Text += known.BearingTo(corrected).DecimalDegrees.ToString(" 000 grader");
            }
        }

        void _interpreter_FixQualityChanged(object sender, FixQualityEventArgs e)
        {
            BeginInvoke(new MethodInvoker(UpdateFixLabel));
        }

        void UpdateFixLabel()
        {
            lock (_syncObject)
            {
                
                lblFixBaseline.Text = "Fix: " + (_baseLineECEF.FixMode == SBP_Enums.FixMode.Fixed_RTK ? "Fixed RTK" : "Float RTK");

                lblFixBaseline.BackColor = _baseLineECEF.FixMode == SBP_Enums.FixMode.Fixed_RTK ? Color.Green : Color.Red;

                if (_positionLLH.FixMode != SBP_Enums.FixMode.SinglePointPosition)
                {
                    lblFixPosition.Text = "Fix: " + _positionLLH.FixMode.ToString();

                    lblFixPosition.BackColor = _positionLLH.FixMode == SBP_Enums.FixMode.Fixed_RTK ? Color.Green : Color.Red;
                }
                lblFixEstimation.Text = "Fix estimation: " + _baseLineECEF.AccuracyEstimate.ToString("0 mm");
                lblIAR.Text = "IAR hypotesis: " + _iarState.NumberOfHypothesis.ToString();
                lblSats.Text = "Number of sats: " + _baseLineECEF.NumberOfSattelites.ToString();
            }
        }

        void UpdateDebugListbox()
        {
            if(chkBoxDebug.Checked)
                lock (_syncObject)
                    while (_messageQueue.Count > 0)
                        txtBoxDebug.AppendText(_messageQueue.Dequeue());
        }

        void UpdateCpuGrid()
        {
            lock(_syncObject)
            {
                while(_threadQueue.Count>0)
                {
                    bool exist = false;
                    ThreadState threadState = _threadQueue.Dequeue();
                    for (int i = 0; i < dataGridCpu.Rows.Count; i++)
                    {
                        if ((string)dataGridCpu.Rows[i].Cells[0].Value == threadState.Name)
                        {
                            dataGridCpu.Rows[i].Cells[1].Value = ((float)threadState.CPU / 10);
                            dataGridCpu.Rows[i].Cells[2].Value = threadState.StackFree;
                            exist = true;
                            break;
                        }
                    }
                    if(!exist)
                        dataGridCpu.Rows.Add(threadState.Name, (threadState.CPU / 10).ToString(), threadState.StackFree.ToString());
                }

                if (_uartState.HasValue)
                {
                    lblUARTACrcErr.Text = "CRC Errors: " + _uartState.Value.UARTA.CRCErrorCount.ToString();
                    lblUARTACrcErr.Text = "CRC Errors: " + _uartState.Value.UARTA.CRCErrorCount.ToString();
                    lblUARTBCrcErr.Text = "CRC Errors: " + _uartState.Value.UARTB.CRCErrorCount.ToString();
                    lblUARTFTDICrcErr.Text = "CRC Errors: " + _uartState.Value.FTDI.CRCErrorCount.ToString();
                    lblUARTAIoErr.Text = "IO Errors: " + _uartState.Value.UARTA.IOErrorCount.ToString();
                    lblUARTBIoErr.Text = "IO Errors: " + _uartState.Value.UARTB.IOErrorCount.ToString();
                    lblUARTFTDIIoErr.Text = "IO Errors: " + _uartState.Value.FTDI.IOErrorCount.ToString();
                    lblUARTATxBuff.Text = "TX Buffer %: " + ((float)_uartState.Value.UARTA.TxBufferLevel / 10).ToString();
                    lblUARTBTxBuff.Text = "TX Buffer %: " + ((float)_uartState.Value.UARTB.TxBufferLevel / 10).ToString();
                    lblUARTFTDITxBuff.Text = "TX Buffer %: " + ((float)_uartState.Value.FTDI.TxBufferLevel / 10).ToString();
                    lblUARTARxBuff.Text = "RX Buffer %: " + ((float)_uartState.Value.UARTA.RxBufferLevel / 10).ToString();
                    lblUARTBRxBuff.Text = "RX Buffer %: " + ((float)_uartState.Value.UARTB.RxBufferLevel / 10).ToString();
                    lblUARTFTDIRxBuff.Text = "RX Buffer %: " + ((float)_uartState.Value.FTDI.RxBufferLevel / 10).ToString();
                    lblUARTATxKbytes.Text = "TX KBytes/s: " + _uartState.Value.UARTA.TxThroughput.ToString("0.00");
                    lblUARTBTxKbytes.Text = "TX KBytes/s: " + _uartState.Value.UARTB.TxThroughput.ToString("0.00");
                    lblUARTFTDITxKbytes.Text = "TX KBytes/s: " + _uartState.Value.FTDI.TxThroughput.ToString("0.00");
                    lblUARTARxKbytes.Text = "RX KBytes/s: " + _uartState.Value.UARTA.RxThroughput.ToString("0.00");
                    lblUARTBRxKbytes.Text = "RX KBytes/s: " + _uartState.Value.UARTB.RxThroughput.ToString("0.00");
                    lblUARTFTDIRxKbytes.Text = "RX KBytes/s: " + _uartState.Value.FTDI.RxThroughput.ToString("0.00");

                    lblObsLatency.Text = "OBS Latency: " + _uartState.Value.Current.ToString();
                    lblObsLatencyAvg.Text = "OBS Latency (Avg ms): " + _uartState.Value.Average.ToString();
                    lblObsLatencyMin.Text = "OBS Latency (Min ms): " + _uartState.Value.Min.ToString();
                    lblObsLatencyMax.Text = "OBS Latency (Max ms): " + _uartState.Value.Max.ToString();
                }
            }
            if (dataGridCpu.SortedColumn != null)
                dataGridCpu.Sort(dataGridCpu.SortedColumn, dataGridCpu.SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
        }

        void _interpreter_PositionChanged(object sender, PositionEventArgs e)
        {
            lock (_syncObject)
            {
                if (_lastSecond != DateTime.Now.Second)
                {
                    _lastSecond = DateTime.Now.Second;
                    _positionCount = 1;
                }
                else
                {
                    _positionCount++;
                    return;
                }

                //AverageFilter filter = _interpreter.Filter as AverageFilter;
                //if (filter.Sampled)
                //    _actualPosition = filter.FilteredPosition;
                //else
                //    _actualPosition = e.Position;

                _actualPositionBaseline = e.Position;
                if (_lastCorrection.HasValue)
                {       
                    Position knownPos = new Position(new Latitude(_lastCorrection.Value.LatitudeKnown), new Longitude(_lastCorrection.Value.LongitudeKnown));
                    Position actPos = new Position(new Latitude(_lastCorrection.Value.LatitudeActual), new Longitude(_lastCorrection.Value.LongitudeActual));
                    Azimuth bearing = actPos.BearingTo(knownPos).Subtract(0).Normalize();
                    Distance distance = actPos.DistanceTo(knownPos);
                    _corrBearing = bearing;
                    _corrDist = distance.Divide(2.5);
                    //_correctedPosition = new Position(new Longitude(_actualPosition.Longitude.DecimalDegrees + _lastCorrection.Value.LongitudeCorrection),
                    //new Latitude(_actualPosition.Latitude.DecimalDegrees + _lastCorrection.Value.LatitudeCorrection));
                }

                BeginInvoke(new MethodInvoker(UpdateLabels));
            }
        }

        private void UpdateLabels()
        {
            lock (_syncObject)
            {
                if (chkBoxActualBaseline.Checked)
                {
                    txtBoxActualPositionBaseline.Text = _actualPositionBaseline.Latitude.DecimalDegrees.ToString() + ";" + _actualPositionBaseline.Longitude.DecimalDegrees.ToString() + ";";
                    txtBoxActualPositionBaseline1.Text = _actualPositionBaseline.Latitude.DecimalDegrees.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US")) + " " + _actualPositionBaseline.Longitude.DecimalDegrees.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                }

                if(chkBoxActual.Checked && _positionLLH.FixMode != SBP_Enums.FixMode.SinglePointPosition)
                {
                    Position position = new Position(new Longitude(_positionLLH.PosLongitude), new Latitude(_positionLLH.PosLatitude));
                    txtBoxActualPosition.Text = position.Latitude.DecimalDegrees.ToString() + ";" + position.Longitude.DecimalDegrees.ToString() + ";";
                    txtBoxActualPosition1.Text = position.Latitude.DecimalDegrees.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US")) + " " + position.Longitude.DecimalDegrees.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                }

                if (chkBoxActualBaselineECEF.Checked)
                    txtBoxActualBaselineECEF.Text = String.Format("{0};{1};{2}", _baseLineECEF.BaselineXMeters.ToString("0.00"), _baseLineECEF.BaselineYMeters.ToString("0.00"), _baseLineECEF.BaselineZMeters.ToString("0.00"));

                if (chkBoxActualBaselineNED.Checked)
                    txtBoxActualBaselineNED.Text = String.Format("{0};{1};{2}", _baseLineNED.BaselineNorthMeters.ToString("0.00"), _baseLineNED.BaselineEastMeters.ToString("0.00"), _baseLineNED.BaselineDownMeters.ToString("0.00"));

                if (_pointActRef.HasValue)
                {
                    Distance distance1 = _line1.DistanceTo(_actualPositionBaseline);
                    lblDistance.Text = distance1.ToMeters().Value.ToString("0.00 m");
                    Distance distance2 = _line2.DistanceTo(_actualPositionBaseline);
                    lblDistanceWest.Text = distance2.ToMeters().Value.ToString("0.00 m");
                    Distance distance3 = _line3.DistanceTo(_actualPositionBaseline);
                    lblDistanceEast.Text = distance3.ToMeters().Value.ToString("0.00 m");
                    lblDistanceToPoint.Text = _pointActRef.Value.DistanceTo(_actualPositionBaseline).ToMeters().Value.ToString("0.00 m");
                }

            }
        }

        private void btnSetPoints_Click(object sender, EventArgs e)
        {
            try
            {
                lock (_syncObject)
                {
                    //_corrPosWhenSet = new Position(new Longitude(_lastCorrection.Value.LongitudeActual), new Latitude(_lastCorrection.Value.LatitudeActual));
                    string[] position = txtBoxPosActRef.Text.Split(';');
                    Position pointActRef = new Position(new Latitude(double.Parse(position[0])), new Longitude(double.Parse(position[1])));
                    _pointActRef = pointActRef;

                    _line1 = new Segment(pointActRef, pointActRef.TranslateTo(Azimuth.North, new Distance(40.0, DistanceUnit.Meters)));
                    _line2 = new Segment(_line1.Start.TranslateTo(Azimuth.West, new Distance(10.0, DistanceUnit.Meters)), _line1.End.TranslateTo(Azimuth.West, new Distance(10.0, DistanceUnit.Meters)));
                    _line3 = new Segment(_line1.Start.TranslateTo(Azimuth.East, new Distance(10.0, DistanceUnit.Meters)), _line1.End.TranslateTo(Azimuth.East, new Distance(10.0, DistanceUnit.Meters)));
                }
            }
            catch (Exception e1)
            {
                return;
            }
        }

        private void btnResetIAR_Click(object sender, EventArgs e)
        {
            _sbpReceiverSender.SendMessage(SBP_Enums.MessageTypes.RESET_FILTERS, SBPReceiverSenderBase.SBP_CONSOLE_SENDER_ID, new ResetFilters(SBP_Enums.ResetFilter.IAR));
        }

        private void btnResetDGNSS_Click(object sender, EventArgs e)
        {
            _sbpReceiverSender.SendMessage(SBP_Enums.MessageTypes.RESET_FILTERS, SBPReceiverSenderBase.SBP_CONSOLE_SENDER_ID, new ResetFilters(SBP_Enums.ResetFilter.DGNSS));
        }
        
        private void chkBoxActivateNtrip_CheckedChanged(object sender, EventArgs e)
        {
            _activateNtrip = chkBoxActivateNtrip.Checked;
        }
        
        private void LogPosition(PositionLLH position)
        {
            CsvRow row = new CsvRow();
            row.Add(_gpsTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
            row.Add(position.PosLatitude.ToString((System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"))));
            row.Add(position.PosLongitude.ToString((System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"))));
            row.Add(position.PosHeight.ToString((System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"))));
            row.Add(position.NumberOfSattelites.ToString());
            row.Add(position.FixMode.ToString());
            _posWriter.WriteRow(row);
            CheckFlush();
        }

        private void LogVelocity(VelocityNED velocity)
        {
            CsvRow row = new CsvRow();
            Speed north = Speed.FromMetersPerSecond((double)velocity.VelocityNorth / 1000);
            Speed east = Speed.FromMetersPerSecond((double)velocity.VelocityEast / 1000);
            Speed down = Speed.FromMetersPerSecond((double)velocity.VelocityDown / 1000);
            Speed speed = Speed.FromMetersPerSecond(Math.Sqrt(Math.Pow(north.Value, 2) + Math.Pow(east.Value, 2)));
            row.Add(_gpsTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
            row.Add(north.ToMetersPerSecond().Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")));            
            row.Add(east.ToMetersPerSecond().Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")));         
            row.Add(down.ToMetersPerSecond().Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")));      
            row.Add(speed.ToMetersPerSecond().Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")));
            row.Add(velocity.NumberOfSattelites.ToString());
            _velocityWriter.WriteRow(row);
            CheckFlush();
        }
        
        private void LogBaseline(BaselineNED baselineNED)
        {
            CsvRow row = new CsvRow();
            row.Add(_gpsTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
            row.Add(baselineNED.BaselineNorthMeters.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")));
            row.Add(baselineNED.BaselineEastMeters.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")));
            row.Add(baselineNED.BaselineDownMeters.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB")));
            double distance = Math.Sqrt(Math.Pow(baselineNED.BaselineNorthMeters, 2) + Math.Pow(baselineNED.BaselineEastMeters, 2) + Math.Pow(baselineNED.BaselineDownMeters, 2));
            row.Add(distance.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"))); 
            row.Add(baselineNED.NumberOfSattelites.ToString());
            row.Add(baselineNED.FixMode.ToString());
            row.Add(_hypothesis.ToString());
            _baselineWriter.WriteRow(row);
            CheckFlush();
        }

        private void Log(Print print)
        {
            _logWriter.Write(_gpsTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff "));
            _logWriter.Write(print);
            CheckFlush();
        }

        private void CheckFlush()
        {
            if(_nextFlush < DateTime.Now)
            {
                _posWriter.Flush();
                _baselineWriter.Flush();
                _velocityWriter.Flush();
                _logWriter.Flush();
                _nextFlush = DateTime.Now.AddSeconds(5.0);
            }
        }

        private void UpdateBaseObservations()
        {
            foreach (ObservationHeader header in _observationsBase)
            {
                foreach (Observation observation in header.Observations)
                {
                    bool exist = false;
                    for (int i = 0; i < dataGridViewBase.Rows.Count; i++)
                    {
                        if((int)dataGridViewBase.Rows[0].Cells[0].Value == (int)(observation.PRN + 1))
                        {
                            dataGridViewBase.Rows[i].Cells[1].Value = observation.SNR;
                            exist = true;
                            break;
                        }
                    }
                    if(!exist)
                        dataGridViewBase.Rows.Add((int)(observation.PRN + 1));
                }
            }
            for(int i = 0; i < dataGridViewBase.Rows.Count; i++)
            {
                int prn = (int)dataGridViewBase.Rows[i].Cells[0].Value;
                bool exist = false;
                foreach (ObservationHeader header in _observationsBase)
                {
                    foreach (Observation observation in header.Observations)
                    {
                        if((int)(observation.PRN + 1) == prn)
                        {
                            exist = true;
                            break;
                        }
                    }
                }
                if(!exist)
                {
                    dataGridViewBase.Rows.RemoveAt(i);
                    i--;
                }
            }
            if (dataGridViewBase.SortedColumn != null)
                dataGridViewBase.Sort(dataGridViewBase.SortedColumn, dataGridViewBase.SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
        }

        private void UpdateRoverObservations()
        {
            foreach (ObservationHeader header in _observationsRover)
            {
                foreach (Observation observation in header.Observations)
                {
                    bool exist = false;
                    for (int i = 0; i < dataGridViewRover.Rows.Count; i++)
                    {
                        if ((int)dataGridViewRover.Rows[i].Cells[0].Value == (int)(observation.PRN + 1))
                        {
                            dataGridViewRover.Rows[i].Cells[1].Value = observation.SNR;
                            exist = true;
                            break;
                        }
                    }
                    if (!exist)
                        dataGridViewRover.Rows.Add((int)(observation.PRN + 1), observation.SNR);

                }
            }
            for (int i = 0; i < dataGridViewRover.Rows.Count; i++)
            {
                int prn = (int)dataGridViewRover.Rows[i].Cells[0].Value;
                bool exist = false;
                foreach (ObservationHeader header in _observationsRover)
                {
                    foreach (Observation observation in header.Observations)
                    {
                        if ((int)(observation.PRN + 1) == prn)
                        {
                            exist = true;
                            break;
                        }
                    }
                }
                if (!exist)
                {
                    dataGridViewRover.Rows.RemoveAt(i);
                    i--;
                }
            }
            if (dataGridViewRover.SortedColumn != null)
                dataGridViewRover.Sort(dataGridViewRover.SortedColumn, dataGridViewRover.SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
        }
        
        private void btnReset_Click(object sender, EventArgs e)
        {
            _sbpReceiverSender.SendMessage(SBP_Enums.MessageTypes.RESET, SBPReceiverSenderBase.SBP_CONSOLE_SENDER_ID, new Reset());
        }

    }


    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
    }

    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        public CsvFileWriter(Stream stream)
            : base(stream)
        {
        }

        public CsvFileWriter(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (string value in row)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                    builder.Append(',');
                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                else
                    builder.Append(value);
                firstColumn = false;
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
        }
    }
}
