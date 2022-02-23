using System;
using System.Collections.Generic;
using DotSpatial.Positioning;
using GeoAPI.Geometries;
using DotSpatial.NTSExtension;
using GpsUtilities.Reciever;
using GpsUtilities.HelperClasses;
using GpsUtilities.Filter;

namespace FarmingGPSLib.Vechile
{
    public class Tractor : VechileBase
    {
        private VechileModel _vechileModel;

        private Coordinate _startOfReverse = new Coordinate(double.NaN, double.NaN);

        private List<double> _headingChangeRate = new List<double>(3);

        private double _turnRate = 0.0;
        
        private Vector _vectorCenterRearAxle;

        private Azimuth _prevHeading = Azimuth.Invalid;

        private DateTime _prevTime  = DateTime.MinValue;

        private int _usedModelPositions = 0;

        private bool _firstUpdate = true;

        public Tractor() : base()
        {
        }

        public Tractor(Azimuth offsetDirection, Distance offsetDistance, Distance wheelAxesDistance, Distance attachPointDistance, Azimuth attachPointDirection) : base(offsetDirection, offsetDistance, wheelAxesDistance, attachPointDistance, attachPointDirection)
        {
            CalculateVector();
        }

        public override Coordinate UpdatePosition(IReceiver receiver)
        {
            if (_firstUpdate)
            {
                _prevHeading = receiver.CurrentBearing;
                _prevTime = DateTime.Now;
            }
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
                if (_startOfReverse.Distance(receiver.CurrentCoordinate) > 20.0 || heading.IsBetween(reverseHeading.Subtract(45.0), reverseHeading.Add(45.0)) || receiver.CurrentSpeed.ToKilometersPerHour().Value > 4.0 )
                    _startOfReverse = new Coordinate(double.NaN, double.NaN);
                else
                    heading = heading.Mirror().Normalize();
            }

            Vector rotatedVector = _vectorCenterRearAxle.RotateZ(heading.DecimalDegrees);
            Coordinate position = new Vector(receiver.CurrentCoordinate) + rotatedVector;

            if (_vechileModel != null)
            {
                if (_firstUpdate)
                {
                    _vechileModel.Init(position, heading);
                    _position = position;
                    _direction = heading;
                }
                else if (!IsReversing) // if going forward we check if the heading changes beyond the model, if so use positions from model for two iterations
                {
                    if (_usedModelPositions > 3)
                    {
                        _headingChangeRate.Clear();
                        _vechileModel.Init(position, heading);
                        _usedModelPositions = 0;
                    }

                    if (_usedModelPositions > 0)
                    {
                        _vechileModel.UpdateModel(receiver.CurrentSpeed.ToKilometersPerHour().Value, _turnRate);
                        _position = _vechileModel.Position;
                        _direction = _vechileModel.Direction;
                        _usedModelPositions++;
                    }
                    else
                    {
                        _position = position;
                        _direction = heading;
                        TimeSpan deltaTime = DateTime.Now - _prevTime;
                        double headingChange = heading.Subtract(_prevHeading).DecimalDegrees;
                        if (headingChange > 180.0)
                            headingChange -= 360.0;
                        else if (headingChange < -180.0)
                            headingChange += 360.0;

                        headingChange /= deltaTime.TotalSeconds;
                        _headingChangeRate.Insert(0, headingChange);
                        if (_headingChangeRate.Count > 3)
                            _headingChangeRate.RemoveAt(3);

                        if (_headingChangeRate.Count > 2)
                        {
                            double _headingChangeRateAvg = 0.0;
                            for (int i = 0; i < _headingChangeRate.Count; i++)
                                _headingChangeRateAvg += _headingChangeRate[i];
                            _headingChangeRateAvg /= _headingChangeRate.Count;
                            _turnRate = _vechileModel.CalculateSteeringAngle(receiver.CurrentSpeed.ToKilometersPerHour().Value, _headingChangeRateAvg);
                            _vechileModel.UpdateModel(receiver.CurrentSpeed.ToKilometersPerHour().Value, _turnRate);

                            double headingDiff = _vechileModel.Direction.DecimalDegrees - receiver.CurrentBearing.DecimalDegrees;
                            if (headingDiff > 180.0)
                                headingDiff -= 360.0;
                            else if(headingDiff < -180.0)
                                headingDiff += 360.0;
                            if (Math.Abs(headingDiff) > 10.0 && _turnRate < 10.0 && _turnRate > -10.0)
                            {
                                _usedModelPositions = 1;
                                _position = _vechileModel.Position;
                                _direction = _vechileModel.Direction;
                            }
                            else if (position.Distance(_vechileModel.Position) > 0.3)
                            {
                                _headingChangeRate.Clear();
                                _vechileModel.Init(position, heading);
                            }
                        }
                    }
                }
                else
                {
                    _headingChangeRate.Clear();
                    _vechileModel.Init(position, heading);
                    _position = position;
                    _direction = heading;
                }
            }
            else
            {
                _position = position;
                _direction = heading;
            }

            _prevHeading = receiver.CurrentBearing;
            _prevTime = DateTime.Now;
            _firstUpdate = false;
            _attachPosition = HelperClassCoordinate.ComputePoint(_position, HelperClassAngles.GetCartesianAngle(VechileDirection - AttachPointDirection).Radians, AttachPointDistance.ToMeters().Value);
            return _position;
        }

        public override bool IsReversing
        {
            get { return !_startOfReverse.IsEmpty(); }
        }

        private void CalculateVector()
        {
            double distanceCenterRearAxle = OffsetDistance.ToMeters().Value;
            double directionCenterRearAxle = HelperClassAngles.GetCartesianAngle(OffsetDirection).Radians;
            Coordinate origin = new Coordinate(0.0, 0.0, 0, 0);
            Coordinate centerRearAxle = HelperClassCoordinate.ComputePoint(origin, directionCenterRearAxle, distanceCenterRearAxle);
            centerRearAxle.Z = 0.0;
            _vechileModel = new VechileModel(WheelAxesDistance.ToMeters().Value);
            _vectorCenterRearAxle = new Vector(origin, centerRearAxle);
        }

        public override void RestoreObject(object restoredState)
        {
            base.RestoreObject(restoredState);
            CalculateVector();
        }
    }
}
