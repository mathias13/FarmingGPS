using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.Equipment.BogBalle
{
    public class L2Plus : EquipmentBase , IEquipmentControl
    {
        private readonly double[,] STOP_START_DISTANCES = new double[2, 3] { { 12, 0, 10 }, { 24, -6, 10 } };

        private double _stopDistance = 0.0;

        private double _startDistance = 0.0;

        private Calibrator _calibrator;

        public L2Plus(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Calibrator calibrator)
            : base(width, distanceFromVechile, fromDirectionOfTravel)
        {
            _calibrator = calibrator;
            _calibrator.ChangeWidth((float)Width.ToMeters().Value);
            SetDistances();
        }

        public L2Plus(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap, Calibrator calibrator)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap)
        {
            _calibrator = calibrator;
            _calibrator.ChangeWidth((float)Width.ToMeters().Value);
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
            _calibrator.Start();
        }

        public void Stop()
        {
            _calibrator.Stop();
        }

        #endregion

    }
}
