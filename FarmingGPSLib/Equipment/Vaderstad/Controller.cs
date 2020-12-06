using System;
using System.Collections.Generic;
using System.Text;
using FarmingGPSLib.Equipment.Win32;
using System.Threading;
using log4net;
using System.Runtime.InteropServices;
using System.Linq;

namespace FarmingGPSLib.Equipment.Vaderstad
{
    public class Controller : IDisposable
    {
        private struct ReadMessage
        {
            public ReadMessage(string command)
            {
                Command = command;
                ReturnValue = String.Empty;
                Finished = false;
                Success = false;
            }

            public string Command { get; set; }
            public string ReturnValue { get; set; }
            public bool Finished { get; set; }
            public bool Success { get; set; }
        }

        private struct WriteMessage
        {
            public WriteMessage(string command, string value)
            {
                Command = command;
                Value = value;
                Finished = false;
                Success = false;
            }

            public string Command { get; set; }
            public string Value { get; set; }
            public bool Finished { get; set; }
            public bool Success { get; set; }
        }

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Private Variables

        //Message constants
        private const string START_CHAR = "{";

        private const string END_CHAR = "}";

        private const string COMMAND_INIT = "S";

        private const string COMMAND_ANSWER = "A";

        private const string READ_INIT = "R";

        private const string READ_ANSWER = "W";

        //Commands
        private const string RATE_COMMAND = "D";

        private const string RATE_TEST_COMMAND = "R";

        private const string RESET_SUM_COMMAND = "Q";

        private const string START_COMMAND = "G";

        private const string STOP_COMMAND = "S";

        private const string SPEED_COMMAND = "V";

        private const string CAL_WEIGHT_COMMAND = "T";

        //Readout
        private const string SET_VALUE_READ = "D";

        private const string ACT_VALUE_READ = "A";
        
        private const string DISTANCE_READ = "L";

        private const string HA_READ = "H";
        
        private const string SPEED_READ = "V";

        private const string SEED_MOTOR_SPEED_READ = "X";

        private const string CAL_WEIGHT_READ = "T";

        private const string SEED_USED_READ = "C";

        private const string STATUS_READ = "S";

        private const string MAX_RATE_OF_TRAVEL = "M";

        private const int DISCONNECTED_COUNT = 10;
        
        private string _comPort = String.Empty;
        
        private object _syncObject = new object();

        private Thread _receiveSendThread;

        private bool _receiveSendThreadStopped = false;

        private Thread _readThread;

        private bool _readThreadStop = false;

        private LinkedList<WriteMessage> _writeMessages = new LinkedList<WriteMessage>();

        private ReadMessage _readMessage;

        private int _readInterval = 1000;

        private int _noAnswerCount = DISCONNECTED_COUNT;

        private int _setValue = -1;

        private int _actValue = -1;
        
        private int _distance = -1;

        private float _ha = -1.0f;
        
        private float _speed = -1.0f;

        private float _seedMotorSpeed = -1.0f;

        private float _maxRateOfTravel = -1.0f;

        private float _seedUsed = -1.0f;

        private float _calWeight = -1.0f;

        private bool _started = false;

        private bool _halted = false;

        private bool _rateTestStarted = false;

        private bool _alarm = false;

        #endregion

        #region Events

        public event EventHandler ValuesUpdated;

        public event EventHandler<bool> IsConnectedChanged;

        #endregion

        #region ctor

        public Controller(string comPort, int readInterval)
        {
            lock (_syncObject)
            {
                _readMessage = new ReadMessage();
                _readMessage.Finished = true;
                _comPort = comPort;
                _readInterval = readInterval;
                _receiveSendThread = new Thread(new ThreadStart(ReceiveSendThreadSerial));
                _receiveSendThread.Start();
                _readThread = new Thread(new ThreadStart(ReadThread));
                _readThread.Start();
            }
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            _readThreadStop = true;
            _readThread.Join();
            _receiveSendThreadStopped = true;
            _receiveSendThread.Join();
        }

        public void Start()
        {
            WriteValue(START_COMMAND, string.Empty);
        }

        public void Stop()
        {
            WriteValue(STOP_COMMAND, string.Empty);
        }

        public void ResetSums()
        {
            WriteValue(RESET_SUM_COMMAND, string.Empty);
        }

        public void StartRateTest()
        {
            WriteValue(RATE_TEST_COMMAND, string.Empty);
        }

        public void ChangeSeedingRate(int rate)
        {
            WriteValue(RATE_COMMAND, (rate * 10).ToString("0000"));
        }

        public void ChangeCalibrationWeight(float weight)
        {
            WriteValue(CAL_WEIGHT_COMMAND, (weight * 100).ToString("0000"));
        }

        public void SetSpeed(float speed)
        {
            WriteValue(SPEED_COMMAND, (speed * 10).ToString("0000"));
        }

        #endregion

        #region Public Properties

        public bool IsConnected
        {
            get
            {
                lock (_syncObject)
                    return _noAnswerCount < DISCONNECTED_COUNT;
            }
        }

        public int ActualSeedingRate
        {
            get { return _actValue; }
        }
            
        public int SetSeedingRate
        {
            get { return _setValue; }
        }

        public float Distance
        {
            get { return _distance; }
        }

        public float HA
        {
            get { return _ha; }
        }

        public float MaxRateOfTravel
        {
            get { return _maxRateOfTravel; }
        }

        public float Speed
        {
            get { return _speed; }
        }

        public float SeedMotorSpeed
        {
            get { return _seedMotorSpeed; }
        }

        public float SeedUsed
        {
            get { return _seedUsed; }
        }

        public float CalibrationWeight
        {
            get { return _calWeight; }
        }

        public bool Started
        {
            get { return _started; }
        }

        public bool Halted
        {
            get { return _halted; }
        }

        public bool RateTestStarted
        {
            get { return _rateTestStarted; }
        }

        public bool Alarm
        {
            get { return _alarm; }
        }

        #endregion

        #region Private Methods

        private void ReceiveSendThreadSerial()
        {
            byte[] buffer = new byte[32];
            IntPtr portHandle = IntPtr.Zero;
            Thread.Sleep(1000);
            WriteMessage writeMessage = new WriteMessage();
            writeMessage.Finished = true;

            while (!_receiveSendThreadStopped)
            {
                try
                {
                    if (portHandle == IntPtr.Zero)
                    {
                        int portnumber = Int32.Parse(_comPort.Replace("COM", String.Empty));
                        string comPort = _comPort;
                        if (portnumber > 9)
                            comPort = String.Format("\\\\.\\{0}", _comPort);
                        portHandle = Win32Com.CreateFile(comPort, Win32Com.GENERIC_READ | Win32Com.GENERIC_WRITE, 0, IntPtr.Zero,
                            Win32Com.OPEN_EXISTING, 0, IntPtr.Zero);

                        if (portHandle == (IntPtr)Win32Com.INVALID_HANDLE_VALUE)
                        {
                            if (Marshal.GetLastWin32Error() == Win32Com.ERROR_ACCESS_DENIED)
                                throw new Exception(String.Format("Access denied for port {0}", _comPort));
                            else
                                throw new Exception(String.Format("Failed to open port {0}", _comPort));
                        }

                        COMMTIMEOUTS commTimeouts = new COMMTIMEOUTS();
                        commTimeouts.ReadIntervalTimeout = uint.MaxValue;
                        commTimeouts.ReadTotalTimeoutConstant = 0;
                        commTimeouts.ReadTotalTimeoutMultiplier = 0;
                        commTimeouts.WriteTotalTimeoutConstant = 0;
                        commTimeouts.WriteTotalTimeoutMultiplier = 0;
                        DCB dcb = new DCB();
                        dcb.Init(false, false, false, 0, false, false, false, false, 0);
                        dcb.BaudRate = 9600;
                        dcb.ByteSize = 8;
                        dcb.Parity = 0;
                        dcb.StopBits = 0;
                        if (!Win32Com.SetupComm(portHandle, 8192, 4096))
                            throw new Exception(String.Format("Failed to set queue settings for port {0}", _comPort));
                        if (!Win32Com.SetCommState(portHandle, ref dcb))
                            throw new Exception(String.Format("Failed to set comm settings for port {0}", _comPort));
                        if (!Win32Com.SetCommTimeouts(portHandle, ref commTimeouts))
                            throw new Exception(String.Format("Failed to set comm timeouts for port {0}", _comPort));
                    }

                    uint lpdwFlags = 0;
                    if (!Win32Com.GetHandleInformation(portHandle, out lpdwFlags))
                        throw new Exception(String.Format("Port {0} went offline", _comPort));


                    if (_writeMessages.Count > 0 && writeMessage.Finished)
                    {
                        lock (_syncObject)
                        {
                            writeMessage = _writeMessages.First();
                            _writeMessages.RemoveFirst();
                        }
                    }

                    if (!writeMessage.Finished)
                    {
                        byte[] writeBytes = BuildMessage(COMMAND_INIT + writeMessage.Command + writeMessage.Value);

                        uint bytesWritten = 0;
                        DateTime sendTimeout = DateTime.Now.AddMilliseconds(500);
                        while (bytesWritten != writeBytes.Length && DateTime.Now < sendTimeout)
                        {
                            if (!Win32Com.WriteFile(portHandle, writeBytes, (uint)writeBytes.Length, out bytesWritten, IntPtr.Zero))
                            {
                                writeMessage.Finished = true;
                                throw new Exception(String.Format("Failed to write port {0}", _comPort));
                            }
                        }

                        if (bytesWritten == writeBytes.Length)
                        {
                            string answer = string.Empty;
                            DateTime readTimeout = DateTime.Now.AddMilliseconds(500);
                            while (!answer.Contains(END_CHAR) && DateTime.Now < readTimeout)
                            {
                                uint bytesRead = 0;
                                if (!Win32Com.ReadFile(portHandle, buffer, (uint)buffer.Length, out bytesRead, IntPtr.Zero))
                                {
                                    writeMessage.Finished = true;
                                    throw new Exception(String.Format("Failed to read port {0}", _comPort));
                                }

                                if (bytesRead > 0)
                                {
                                    byte[] bytes = new byte[bytesRead];
                                    Buffer.BlockCopy(buffer, 0, bytes, 0, (int)bytesRead);
                                    answer += Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                                }
                            }
                            if (ValidateMessage(ref answer))
                            {
                                if (answer.Contains(COMMAND_ANSWER + writeMessage.Command))
                                    writeMessage.Success = true;
                            }
                            writeMessage.Finished = true;
                        }
                        if (!writeMessage.Success)
                            Log.Warn("Failed to write value with command: " + writeMessage.Command);
                    }
                    else if (!_readMessage.Finished)
                    {
                        byte[] writeBytes = BuildMessage(READ_INIT + _readMessage.Command);

                        uint bytesWritten = 0;
                        DateTime sendTimeout = DateTime.Now.AddMilliseconds(500);
                        while (bytesWritten != writeBytes.Length && DateTime.Now < sendTimeout)
                        {
                            if (!Win32Com.WriteFile(portHandle, writeBytes, (uint)writeBytes.Length, out bytesWritten, IntPtr.Zero))
                            {
                                _readMessage.Finished = true;
                                throw new Exception(String.Format("Failed to write port {0}", _comPort));
                            }
                        }

                        if (bytesWritten == writeBytes.Length)
                        {
                            string answer = string.Empty;
                            DateTime readTimeout = DateTime.Now.AddMilliseconds(500);
                            while (!answer.Contains(END_CHAR) && DateTime.Now < readTimeout)
                            {
                                uint bytesRead = 0;
                                if (!Win32Com.ReadFile(portHandle, buffer, (uint)buffer.Length, out bytesRead, IntPtr.Zero))
                                {
                                    _readMessage.Finished = true;
                                    throw new Exception(String.Format("Failed to read port {0}", _comPort));
                                }

                                if (bytesRead > 0)
                                {
                                    byte[] bytes = new byte[bytesRead];
                                    Buffer.BlockCopy(buffer, 0, bytes, 0, (int)bytesRead);
                                    answer += Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                                }
                            }
                            if (ValidateMessage(ref answer))
                            {
                                string commandAnswer = _readMessage.Command;
                                if (answer.StartsWith(READ_ANSWER + commandAnswer))
                                {
                                    _readMessage.ReturnValue = answer.Replace(READ_ANSWER + commandAnswer, String.Empty);
                                    _readMessage.Success = true;
                                }
                            }
                            _readMessage.Finished = true;
                        }
                    }
                    else
                        Thread.Sleep(1);         
                }
                catch (Exception e)
                {
                    Log.Error("Com port failiure", e);
                    _readMessage.Success = false;
                    _readMessage.Finished = true;
                    _writeMessages.Clear();
                    Win32Com.CancelIo(portHandle);
                    Win32Com.CloseHandle(portHandle);
                    portHandle = IntPtr.Zero;
                    Thread.Sleep(5000);
                }
            }
            Win32Com.CancelIo(portHandle);
            Win32Com.CloseHandle(portHandle);
        }

        private void ReadThread()
        {
            DateTime nextRead = DateTime.MinValue;
            while (!_readThreadStop)
            {
                if(nextRead > DateTime.Now)
                {
                    Thread.Sleep(1);
                    continue;
                }

                nextRead = DateTime.Now.AddMilliseconds(_readInterval);

                bool isConnected = IsConnected;

                string value = ReadValue(STATUS_READ);
                float floatValue = 0f;
                if (value.Length > 0)
                {
                    byte integerValue = Encoding.ASCII.GetBytes(value)[0];
                    _started = ((integerValue & 0x01) > 0);
                    _rateTestStarted = ((integerValue & 0x02) > 0);
                    _alarm = ((integerValue & 0x04) > 0);
                    _halted = ((integerValue & 0x08) > 0);
                }

                if (isConnected)
                {
                    value = ReadValue(ACT_VALUE_READ);
                    if (!int.TryParse(value, out _actValue))
                        _actValue = -1;

                    value = ReadValue(SET_VALUE_READ);
                    if (!int.TryParse(value, out _setValue))
                        _setValue = -1;

                    value = ReadValue(DISTANCE_READ);
                    if (!int.TryParse(value, out _distance))
                        _distance = -1;

                    value = ReadValue(MAX_RATE_OF_TRAVEL);
                    if (!float.TryParse(value, out floatValue))
                        _maxRateOfTravel = -1;
                    else
                        _maxRateOfTravel = floatValue / 10f;

                    value = ReadValue(HA_READ);
                    if (!float.TryParse(value, out floatValue))
                        _ha = -1;
                    else
                        _ha = floatValue / 10f;

                    value = ReadValue(SPEED_READ);
                    if (!float.TryParse(value, out floatValue))
                        _speed = -1;
                    else
                        _speed = floatValue / 10f;

                    value = ReadValue(SEED_MOTOR_SPEED_READ);
                    if (!float.TryParse(value, out floatValue))
                        _seedMotorSpeed = -1;
                    else
                        _seedMotorSpeed = floatValue / 10f;

                    value = ReadValue(SEED_USED_READ);
                    if (!float.TryParse(value, out floatValue))
                        _seedUsed = -1;
                    else
                        _seedUsed = floatValue / 10f;
                    
                    value = ReadValue(CAL_WEIGHT_READ);
                    if (!float.TryParse(value, out floatValue))
                        _calWeight = -1;
                    else
                        _calWeight = floatValue / 100f;

                    if (ValuesUpdated != null)
                        ValuesUpdated.Invoke(this, new EventArgs());
                }

                if (isConnected != IsConnected)
                    if (IsConnectedChanged != null)
                        IsConnectedChanged.Invoke(this, IsConnected);
            }
        }

        private bool ValidateMessage(ref string message)
        {
            if (!message.Contains(START_CHAR) || !message.Contains(END_CHAR))
                return false;

            message = message.Replace(START_CHAR, String.Empty);
            message = message.Replace(END_CHAR, String.Empty);
            byte checksum = Encoding.ASCII.GetBytes(message.Substring(message.Length - 1))[0];
            message = message.Remove(message.Length - 1);
            return checksum == CalculateChecksum(message);                
        }

        private byte CalculateChecksum(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            byte checkSum = bytes[0];
            for (int i = 1; i < bytes.Length; i++)
                checkSum = (byte)((uint)checkSum ^ (uint)bytes[i]);

            if (checkSum == 0x00 || checkSum == 0x7B || checkSum == 0x7D)
                checkSum = 0x55;
            return checkSum;
        }
        
        private byte[] BuildMessage(string messageContent)
        {
            byte checksum = CalculateChecksum(messageContent);
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes(messageContent));
            bytes.Add(checksum);
            bytes.AddRange(Encoding.ASCII.GetBytes(END_CHAR));
            bytes.InsertRange(0, Encoding.ASCII.GetBytes(START_CHAR));
            return bytes.ToArray();
        }

        private string ReadValue(string command)
        {
            _readMessage = new ReadMessage(command);

            while (!_readMessage.Finished)
                Thread.Sleep(1);

            if (_readMessage.Success)
            {
                _noAnswerCount = 0;
                return _readMessage.ReturnValue;
            }
            else
            {
                _noAnswerCount++;
                Log.Warn("Failed to read value with command: " + command);
                return String.Empty;
            }
        }
            
        private void WriteValue(string command, string value)
        {
            if (!IsConnected)
                return;

            if (command == START_COMMAND)
                lock (_syncObject)
                    _writeMessages.AddFirst(new WriteMessage(command, value));
            else
                lock (_syncObject)
                    _writeMessages.AddLast(new WriteMessage(command, value));
        }

        #endregion
    }
}
