using System;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using GpsUtilities.Reciever;
using GpsUtilities.HelperClasses;
using GpsUtilities.Filter;

namespace FarmingGPSLib.Vechile
{
    public class Tractor : VechileBase
    {
        private VechileModel _vechileModel;

        private Coordinate _startOfReverse = Coordinate.Empty;

        private double _distanceCenterRearAxle = 0.0;

        private double _directionCenterRearAxle = 0.0;

        private Vector _vectorCenterRearAxle;

        private Azimuth _prevHeading = Azimuth.Invalid;

        public Tractor() : base()
        {

        }

        public Tractor(Azimuth offsetDirection, Distance offsetDistance) : base(offsetDirection, offsetDistance)
        {
            _distanceCenterRearAxle = offsetDistance.ToMeters().Value;
            _directionCenterRearAxle = HelperClassAngles.GetCartesianAngle(offsetDirection).Radians;
            Coordinate origin = new Coordinate(0.0, 0.0, 0, 0);
            Coordinate centerRearAxle = HelperClassCoordinate.ComputePoint(origin, _directionCenterRearAxle, _distanceCenterRearAxle);
            centerRearAxle.Z = 0.0;
            _vechileModel = new VechileModel(2.2, 2.56);
            _vectorCenterRearAxle = new Vector(origin, centerRearAxle);
        }

        public override Coordinate UpdatePosition(IReceiver receiver)
        {
            if (_prevHeading.IsInvalid)
                _prevHeading = receiver.CurrentBearing;
            Azimuth heading = receiver.CurrentBearing;
            Azimuth reverseHeading = _prevHeading.Mirror();

            if (_startOfReverse.IsEmpty())
            {
                if (heading.IsBetween(reverseHeading.Subtract(45.0), reverseHeading.Add(45.0)))
                {
                    _startOfReverse = receiver.CurrentCoordinate;
                    heading = heading.Mirror().Normalize();
                }
            }
            else
            {
                if (_startOfReverse.Distance(receiver.CurrentCoordinate) > 15.0 || heading.IsBetween(reverseHeading.Subtract(45.0), reverseHeading.Add(45.0)))
                {
                    _startOfReverse = Coordinate.Empty;
                }
                else
                    heading = heading.Mirror().Normalize();
            }

            Vector rotatedVector = _vectorCenterRearAxle.RotateZ(heading.DecimalDegrees * -1);
            Coordinate position = receiver.CurrentCoordinate + rotatedVector;

            if (_vechileModel != null)
            {
                _position = position;
                _direction = heading;
                //_vechileModel.UpdateModel(receiver.CurrentCoordinate, receiver.CurrentSpeed.ToKilometersPerHour().Value, receiver.CurrentBearing);
                //_position = _vechileModel.Position;
                //_direction = _vechileModel.Direction;
            }
            else
            {
                _position = position;
                _direction = heading;
            }

            _prevHeading = receiver.CurrentBearing;
            return _position;
        }

        public override bool IsReversing
        {
            get { return !_startOfReverse.IsEmpty(); }
        }
    }
}
