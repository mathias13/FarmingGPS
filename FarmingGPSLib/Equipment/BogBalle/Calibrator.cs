using System;
using System.Collections.Generic;
using System.Text;
using FarmingGPSLib.Equipment.Win32;
using System.Threading;
using log4net;
using System.Runtime.InteropServices;

namespace FarmingGPSLib.Equipment.BogBalle
{
    public class Calibrator : IDisposable
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

        private const string START_COMMAND = "G";

        private const string STOP_COMMAND = "S";

        //Commands
        private const string RATE_COMMAND = "D";

        private const string SPREAD_WIDTH_COMMAND = "B";

        //Readout
        private const string SET_VALUE_READ = "D";

        private const string ACT_VALUE_READ = "A";

        private const string SPREAD_WIDTH_READ = "B";

        private const string DISTANCE_READ = "L";

        private const string HA_READ = "H";

        private const string HOPPER_CONTENTS_READ = "I";

        private const string TARA_READ = "T";

        private const string SPEED_READ = "V";

        private const string PTO_READ = "P";

        private const string STATUS_READ = "S";

        private const int DISCONNECTED_COUNT = 10;
        
        private string _comPort = String.Empty;
        
        private object _syncObject = new object();

        private Thread _receiveSendThread;

        private bool _receiveSendThreadStopped = false;

        private Thread _readThread;

        private bool _readThreadStop = false;

        private WriteMessage _writeMessage;

        private ReadMessage _readMessage;

        private int _readInterval = 1000;

        private int _noAnswerCount = DISCONNECTED_COUNT;

        private int _setValue = -1;

        private int _actValue = -1;

        private float _spreadWidth = -1.0f;

        private int _distance = -1;

        private float _ha = -1.0f;

        private int _hopperContents = -1;

        private float _speed = -1.0f;

        private int _tara = -1;

        private int _pto = -1;

        private bool _started = false;

        private int _activeField = -1;

        #endregion

        #region Events
            
        public event EventHandler ValuesUpdated;

        public event EventHandler<bool> IsConnectedChanged;

        #endregion

        #region ctor

        public Calibrator(string comPort, int readInterval)
        {
            lock (_syncObject)
            {
                _readMessage = new ReadMessage();
                _readMessage.Finished = true;
                _writeMessage = new WriteMessage();
                _writeMessage.Finished = true;
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

        public bool Start()
        {
            return WriteValue(START_COMMAND, string.Empty);
        }

        public bool Stop()
        {
            return WriteValue(STOP_COMMAND, string.Empty);
        }
        
        public bool ChangeWidth(float width)
        {
            return WriteValue(SPREAD_WIDTH_COMMAND, (width * 10).ToString("000"));
        }

        public bool ChangeSpreadingRate(int rate)
        {
            return WriteValue(RATE_COMMAND, rate.ToString("0000"));
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

        public int ActualSpreadingRate
        {
            get { return _actValue; }
        }
            
        public int SetSpreadingRate
        {
            get { return _setValue; }
        }

        public float SpreadWidth
        {
            get { return _spreadWidth; }
        }

        public int Distance
        {
            get { return _distance; }
        }

        public float HA
        {
            get { return _ha; }
        }

        public int HopperContents
        {
            get { return _hopperContents; }
        }

        public float Speed
        {
            get { return _speed; }
        }

        public int Tara
        {
            get { return _tara; }
        }

        public int PTO
        {
            get { return _pto; }
        }

        public bool Started
        {
            get { return _started; }
        }

        #endregion

        #region Private Methods

        private void ReceiveSendThreadSerial()
        {
            byte[] buffer = new byte[32];
            IntPtr portHandle = IntPtr.Zero;
            Thread.Sleep(1000);

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

                    if (!_writeMessage.Finished)
                    {
                        byte[] writeBytes = BuildMessage(COMMAND_INIT + _writeMessage.Command + _writeMessage.Value);

                        uint bytesWritten = 0;
                        DateTime sendTimeout = DateTime.Now.AddMilliseconds(500);
                        while (bytesWritten != writeBytes.Length && DateTime.Now < sendTimeout)
                        {
                            if (!Win32Com.WriteFile(portHandle, writeBytes, (uint)writeBytes.Length, out bytesWritten, IntPtr.Zero))
                            {
                                _writeMessage.Finished = true;
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
                                    _writeMessage.Finished = true;
                                    throw new Exception(String.Format("Failed to read port {0}", _comPort));
                                }

                                if (bytesRead > 0)
                                {
                                    byte[] bytes = new byte[bytesRead];
                                    Buffer.BlockCopy(buffer, 0, bytes, 0, (int)bytesRead);
                                }
                            }
                            if (ValidateMessage(ref answer))
                            {
                                if (answer.Contains(COMMAND_ANSWER + _writeMessage.Command))
                                    _writeMessage.Success = true;
                            }
                            _writeMessage.Finished = true;
                        }
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
                                }
                            }
                            if (ValidateMessage(ref answer))
                            {
                                string commandAnswer = _readMessage.Command;
                                if (_readMessage.Command == STATUS_READ)
                                    commandAnswer = "P";
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
                if (value.Length > 3)
                {
                    if (int.TryParse(value.Substring(3, 1), out _activeField))
                        _started = value[2] == '1';
                    else
                    {
                        _started = false;
                        _activeField = -1;
                    }
                }

                value = ReadValue(ACT_VALUE_READ);
                if (!int.TryParse(value, out _actValue))
                    _actValue = -1;

                value = ReadValue(SET_VALUE_READ);
                if (!int.TryParse(value, out _setValue))
                    _setValue = -1;

                value = ReadValue(SPREAD_WIDTH_READ);
                if (float.TryParse(value, out _spreadWidth))
                    _spreadWidth = _spreadWidth / 10;
                else
                    _spreadWidth = -1.0f;

                if (_activeField > -1)
                {
                    value = ReadValue(HA_READ + _activeField.ToString());
                    if (float.TryParse(value, out _ha))
                        _ha = _ha / 100;
                    else
                        _ha = -1.0f;
                }

                value = ReadValue(TARA_READ);
                if (!int.TryParse(value, out _tara))
                    _tara = -1;

                value = ReadValue(SPEED_READ);
                if (float.TryParse(value, out _speed))
                    _speed = _speed / 10;
                else
                    _speed = -1.0f;

                value = ReadValue(PTO_READ);
                if (!int.TryParse(value, out _pto))
                    _pto = -1;

                if (ValuesUpdated != null)
                    ValuesUpdated.Invoke(this, new EventArgs());

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
            lock (_syncObject)
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
        }
            
        private bool WriteValue(string command, string value)
        {
            _writeMessage = new WriteMessage(command, value);

            while (!_writeMessage.Finished)
                Thread.Sleep(1);

            if (_writeMessage.Success)
            {
                _noAnswerCount = 0;
                return true;
            }
            else
            {
                _noAnswerCount++;
                Log.Warn("Failed to write value with command: " + command);
                return false;
            }
        }

        #endregion
    }
}
