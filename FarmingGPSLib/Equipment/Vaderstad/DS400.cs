using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;
using FarmingGPSLib.Settings;
using FarmingGPSLib.Vechile;

namespace FarmingGPSLib.Equipment.Vaderstad
{
    public class DS400 : EquipmentBase, IEquipmentControl, IEquipmentStat, IDisposable
    {
        private static double FULL_CONTENT = 700.0;

        private readonly double _stopDistance = 0.8;

        private readonly double _startDistance = -0.7;

        private double _startWeight = double.MinValue;

        private double _endWeight = 0.0;

        private double _prevContent = double.MinValue;

        private Controller _controller;
        
        public DS400()
        {
        }

        public DS400(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, vechile)
        {
        }

        public DS400(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap, vechile)
        {
        }

        private void _controller_ValuesUpdated(object sender, EventArgs e)
        {
            if (_startWeight == double.MinValue)
            {
                _startWeight = _controller.SeedUsed;
                _endWeight = _startWeight;
            }

            if (_prevContent > 10.0 && ContentLeft <= 10.0)
                FarmingEvent.Invoke(this, "Mindre än 10% i såmaskinen");

            if (StatUpdated != null)
                StatUpdated.Invoke(this, new EventArgs());

            if (StatusUpdate != null)
                StatusUpdate.Invoke(this, new EventArgs());

            _prevContent = ContentLeft;
        }

        private void _controller_IsConnectedChanged(object sender, bool e)
        {
            if (StatusUpdate != null)
                StatusUpdate.Invoke(this, new EventArgs());
        }

        #region IDisposable interface

        public void Dispose()
        {
            _controller.ValuesUpdated -= _controller_ValuesUpdated;
            _controller.Dispose();
        }

        #endregion

        #region IEquipmentControl interface

        public event EventHandler StatusUpdate;

        public double StartDistance
        {
            get
            {
                return _startDistance;
            }
        }

        public double StopDistance
        {
            get
            {
                return _stopDistance;
            }
        }

        public void Start()
        {
            if (_controller != null)
                _controller.Start();
        }

        public void Stop()
        {
            if (_controller != null)
                _controller.Stop();
        }

        public bool Running
        {
            get
            {
                return _controller.Started;
            }
        }

        public bool Connected
        {
            get
            {
                return _controller.IsConnected;
            }
        }

        public Type ControllerSettingsType
        {
            get { return typeof(Settings.Vaderstad.Controller); }
        }

        public Type ControllerType
        {
            get { return typeof(Controller); }
        }

        public object RegisterController(object settings)
        {
            if (settings is Settings.Vaderstad.Controller)
            {
                Settings.Vaderstad.Controller controllerSettings = settings as Settings.Vaderstad.Controller;
                if (_controller != null)
                    _controller.Dispose();
                _controller = new Controller(controllerSettings.COMPort, controllerSettings.ReadInterval);
                _controller.ValuesUpdated += _controller_ValuesUpdated;
                _controller.IsConnectedChanged += _controller_IsConnectedChanged;
                return _controller;
            }
            else
                return null;
        }

        public void SetRate(double rate)
        {
            if(_controller != null)
                _controller.ChangeSeedingRate((int)rate);
        }

        public void RelaySpeed(double speed)
        {
            if (_controller != null)
                _controller.SetSpeed((float)speed);
        }

        #endregion

        #region IEquipmentStat interface

        public event EventHandler StatUpdated;

        public event EventHandler<string> FarmingEvent;

        public double Content
        {
            get { return _endWeight - _controller.SeedUsed; }
        }

        public double ContentLeft
        {
            get { return (Content / FULL_CONTENT) * 100.0; }
        }

        public double TotalInput
        {
            get { return _controller.SeedUsed - _startWeight; }
        }

        public double StartWeight
        {
            get { return _startWeight; }
            set { _startWeight = value; }
        }
        public double EndWeight
        {
            get { return _endWeight; }
            set { _endWeight = value; }
        }

        public void ResetTotal()
        {
            _startWeight = _controller.SeedUsed;
            _endWeight = _controller.SeedUsed;
            HasChanged = true;
        }
        
        public void AddedContent(double content)
        {
            _endWeight += content;
            HasChanged = true;
        }

        #endregion

        public override Type FarmingMode
        {
            get
            {
                return typeof(SeedingMode);
            }
        }

    }
}
