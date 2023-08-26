using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;
using FarmingGPSLib.Settings;
using FarmingGPSLib.Vechile;

namespace FarmingGPSLib.Equipment.BogBalle
{
    public class L2Plus : EquipmentBase, IEquipmentControl, IEquipmentStat, IDisposable
    {
        private readonly double[,] STOP_START_DISTANCES = new double[2, 3] { { 12, 1, 6 }, { 24, -5, 6 } };

        private static double FULL_CONTENT = 1500.0;

        private double _stopDistance = 0.0;

        private double _startDistance = 0.0;

        private double _startWeight = double.MinValue;

        private double _endWeight = 0.0;

        private Calibrator _calibrator;
        
        public L2Plus()
        {
        }

        public L2Plus(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, vechile)
        {
            SetDistances();
        }

        public L2Plus(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap, vechile)
        {
            SetDistances();
        }

        private void SetDistances()
        {
            double lastDiffWidth = double.MaxValue;
            for (int i = 0; i < STOP_START_DISTANCES.GetLength(0); i++)
            {
                double diff = Math.Abs((STOP_START_DISTANCES[i, 0] - Width.ToMeters().Value));
                if (diff < lastDiffWidth)
                {
                    lastDiffWidth = diff;
                    _stopDistance = STOP_START_DISTANCES[i, 1];
                    _startDistance = STOP_START_DISTANCES[i, 2];
                }
            }
        }

        private void _calibrator_ValuesUpdated(object sender, EventArgs e)
        {
            if (_startWeight == double.MinValue)
            {
                _startWeight = _calibrator.Tara;
                _endWeight = _startWeight;
            }

            if (StatUpdated != null)
                StatUpdated.Invoke(this, new EventArgs());

            if (StatusUpdate != null)
                StatusUpdate.Invoke(this, new EventArgs());
        }

        private void _calibrator_IsConnectedChanged(object sender, bool e)
        {
            if (StatusUpdate != null)
                StatusUpdate.Invoke(this, new EventArgs());
        }

        #region IDisposable interface

        public void Dispose()
        {
            _calibrator.ValuesUpdated -= _calibrator_ValuesUpdated;
            _calibrator.Dispose();
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
            if (_calibrator != null)
                _calibrator.Start();
        }

        public void Stop()
        {
            if (_calibrator != null)
                _calibrator.Stop();
        }

        public bool Running
        {
            get
            {
                return _calibrator.Started;
            }
        }

        public bool Connected
        {
            get
            {
                return _calibrator.IsConnected;
            }
        }

        public Type ControllerSettingsType
        {
            get { return typeof(Settings.BogBalle.Calibrator); }
        }

        public Type ControllerType
        {
            get { return typeof(Calibrator); }
        }

        public object RegisterController(object settings)
        {
            if (settings is Settings.BogBalle.Calibrator)
            {
                Settings.BogBalle.Calibrator calibratorSettings = settings as Settings.BogBalle.Calibrator;
                if (_calibrator != null)
                    _calibrator.Dispose();
                _calibrator = new Calibrator(calibratorSettings.COMPort, calibratorSettings.ReadInterval);
                _calibrator.ChangeWidth((float)Width.ToMeters().Value);
                _calibrator.ValuesUpdated += _calibrator_ValuesUpdated;
                _calibrator.IsConnectedChanged += _calibrator_IsConnectedChanged;
                return _calibrator;
            }
            else
                return null;
        }

        public void SetRate(double rate)
        {
            if (_calibrator != null)
                _calibrator.ChangeSpreadingRate((int)rate);
        }

        public void RelaySpeed(double speed)
        {
        }

        #endregion

        #region IEquipmentStat interface

        public event EventHandler StatUpdated;

        public double Content
        {
            get { return _endWeight - _calibrator.Tara; }
        }

        public double ContentLeft
        {
            get { return (Content / FULL_CONTENT) * 100.0; }
        }

        public double TotalInput
        {
            get { return _calibrator.Tara - _startWeight; }
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
            _startWeight = _calibrator.Tara;
            _endWeight = _calibrator.Tara;
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
                return typeof(FertilizingMode);
            }
        }

        #region IStateObject
        
        public override void RestoreObject(object restoredState)
        {
            base.RestoreObject(restoredState);
            SetDistances();
        }

        #endregion
    }
}
