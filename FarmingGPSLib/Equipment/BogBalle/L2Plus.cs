using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;
using FarmingGPSLib.Settings;

namespace FarmingGPSLib.Equipment.BogBalle
{
    public class L2Plus : EquipmentBase , IEquipmentControl, IDisposable
    {
        private readonly double[,] STOP_START_DISTANCES = new double[2, 3] { { 12, 0, 10 }, { 24, -6, 10 } };

        private double _stopDistance = 0.0;

        private double _startDistance = 0.0;

        private Calibrator _calibrator;

        public L2Plus(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel)
            : base(width, distanceFromVechile, fromDirectionOfTravel)
        {
            SetDistances();
        }

        public L2Plus(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap)
        {
            SetDistances();
        }

        private void SetDistances()
        {
            double lastDiffWidth = double.MaxValue;
            for(int i = 0; i < STOP_START_DISTANCES.GetLength(0); i++)
            {
                double diff = Math.Abs((STOP_START_DISTANCES[i, 0] - Width.ToMeters().Value));
                if(diff < lastDiffWidth)
                {
                    lastDiffWidth = diff;
                    _stopDistance = STOP_START_DISTANCES[i, 1];
                    _startDistance = STOP_START_DISTANCES[i, 2];
                }
            }                
        }

        #region IDisposable interface

        public void Dispose()
        {
            _calibrator.Dispose();
        }

        #endregion

        #region IEquipmentControl interface

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
                _calibrator = new Calibrator(calibratorSettings.COMPort, calibratorSettings.ReadInterval);
                _calibrator.ChangeWidth((float)Width.ToMeters().Value);
                return _calibrator;
            }
            else
                return null;
        }

        public void SetRate(double rate)
        {
            _calibrator.ChangeSpreadingRate((int)rate);
        }

        #endregion

        public override Type FarmingMode
        {
            get
            {
                return typeof(FertilizingMode);
            }
        }
    }
}
