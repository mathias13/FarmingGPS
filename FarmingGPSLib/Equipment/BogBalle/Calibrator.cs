using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace FarmingGPSLib.Equipment.BogBalle
{
    public class Calibrator : IDisposable
    {
        #region Private Variables

        private const string START_CHAR = "{";

        private const string END_CHAR = "}";

        private const string COMMAND_INIT = "S";

        private const string COMMAND_ANSWER = "A";

        private const string READ_INIT = "R";

        private const string READ_ANSWER = "W";

        private const string START_COMMAND = "G";

        private const string STOP_COMMAND = "S";

        private const string RATE_COMMAND = "D";

        private const string SPREAD_WIDTH_COMMAND = "B";

        private const string SET_VALUE_READ = "D";

        private const string ACT_VALUE_READ = "A";

        private const string SPREAD_WIDTH_READ = "B";

        private const string SPEED_READ = "V";

        private const string PTO_READ = "P";

        private SerialPort _serialPort;

        private object _syncObject = new object();

        private Timer _readTimer;

        private int _noAnswerCount = 0;

        private int _setValue = -1;

        private int _actValue = -1;

        private float _spreadWidth = -1.0f;

        private float _speed = -1.0f;

        private int _pto = -1;

        #endregion

        #region Events
            
        public delegate void ValuesUpdatedDelegate(object sender);

        public event ValuesUpdatedDelegate ValuesUpdated;

        #endregion

        #region ctor

        public Calibrator(string comPort, int readInterval)
        {
            lock (_syncObject)
            {
                _serialPort = new SerialPort(comPort);
                _serialPort.BaudRate = 9600;
                _serialPort.DataBits = 8;
                _serialPort.Parity = Parity.None;
                _serialPort.StopBits = StopBits.One;
                _serialPort.ReadTimeout = 100;
                _serialPort.Handshake = Handshake.None;
                _readTimer = new Timer(new TimerCallback(ReadValues), new object(), 0, readInterval);
            }
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            if (_serialPort != null)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
        }

        public bool Start()
        {
            return WriteValue(START_COMMAND, string.Empty);
        }

        public bool Stop()
        {
            return WriteValue(STOP_COMMAND, string.Empty);
        }
        
        public bool SetWidth(float width)
        {
            return WriteValue(SPREAD_WIDTH_COMMAND, (width * 10).ToString("000"));
        }

        #endregion

        #region Public Properties

        public bool IsConnected
        {
            get
            {
                lock (_syncObject)
                    return _serialPort.IsOpen && _noAnswerCount < 10;
            }
        }

        public int ActualValue
        {
            get { return _actValue; }
        }
            
        public int SetValue
        {
            get { return _setValue; }
        }

        public float SpreadWidth
        {
            get { return _spreadWidth; }
        }

        public float Speed
        {
            get { return _speed; }
        }

        public int PTO
        {
            get { return _pto; }
        }

        #endregion

        #region Private Methods

        private void ReadValues(object state)
        {
            string value = ReadValue(ACT_VALUE_READ);
            if (value != string.Empty)
                _actValue = int.Parse(value);
            else
                _actValue = -1;

            value = ReadValue(SET_VALUE_READ);
            if (value != string.Empty)
                _setValue = int.Parse(value);
            else
                _setValue = -1;

            value = ReadValue(SPREAD_WIDTH_READ);
            if (value != string.Empty)
                _spreadWidth = float.Parse(value) / 10;
            else
                _spreadWidth = -1.0f;

            value = ReadValue(SPEED_READ);
            if (value != string.Empty)
                _speed = float.Parse(value) / 10;
            else
                _speed = -1.0f;

            value = ReadValue(PTO_READ);
            if (value != string.Empty)
                _pto = int.Parse(value);
            else
                _pto = -1;

            if (ValuesUpdated != null)
                ValuesUpdated.Invoke(this);
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
            string answer = string.Empty;
            byte[] bytes = BuildMessage(READ_INIT + command);
            DateTime timeout = DateTime.MinValue;
            lock (_syncObject)
            {
                try
                {
                    if (!_serialPort.IsOpen)
                        _serialPort.Open();

                    _serialPort.Write(bytes, 0, bytes.Length);

                    timeout = DateTime.Now.AddSeconds(1.0);
                    while (!answer.Contains(END_CHAR))
                    {
                        byte[] readBuffer = new byte[10];
                        int bytesRead = _serialPort.Read(readBuffer, 0, 10);
                        if (bytesRead < 1)
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                        else
                        {
                            answer += Encoding.ASCII.GetString(readBuffer, 0, bytesRead);
                        }
                        if (timeout < DateTime.Now)
                            break;
                    }
                    if (ValidateMessage(ref answer))
                    {
                        if (answer.StartsWith(READ_ANSWER + command))
                        {
                            _noAnswerCount = 0;
                            return answer.Replace(READ_ANSWER + command, String.Empty);
                        }
                        else
                        {
                            _noAnswerCount++;
                            return string.Empty;
                        }
                    }
                    else
                    {
                        _noAnswerCount++;
                        return string.Empty;
                    }
                }
                catch(Exception e)
                {
                    return string.Empty;
                }
            }
        }
            
        private bool WriteValue(string command, string value)
        {
            string answer = string.Empty;
            byte[] bytes = BuildMessage(COMMAND_INIT + command + value);
            DateTime timeout = DateTime.MinValue;
            lock (_syncObject)
            {
                try
                {
                    if (!_serialPort.IsOpen)
                        _serialPort.Open();

                    _serialPort.Write(bytes, 0, bytes.Length);

                    timeout = DateTime.Now.AddSeconds(1.0);
                    while (!answer.Contains(END_CHAR))
                    {
                        byte[] readBuffer = new byte[10];
                        int bytesRead = _serialPort.Read(readBuffer, 0, 10);
                        if (bytesRead < 1)
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                        else
                            answer += Encoding.ASCII.GetString(readBuffer, 0, bytesRead);
                        if (timeout < DateTime.Now)
                            break;
                    }
                    if (ValidateMessage(ref answer))
                    {
                        if (answer.Contains(COMMAND_ANSWER + command))
                        {
                            _noAnswerCount = 0;
                            return true;
                        }
                        else
                        {
                            _noAnswerCount++;
                            return true;
                        }
                    }
                    else
                    {
                        _noAnswerCount++;
                        return false;
                    }
                }
                catch(Exception e)
                {                        
                    return false;
                }
            }
        }

        #endregion
    }
}
