using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.Equipment
{
    public class EquipmentBase: IEquipment
    {
        #region Private Variables

        protected Distance _width;

        protected Distance _distanceFromVechile;

        protected Distance _overlap;

        protected Azimuth _fromDirectionOfTravel;

        protected Azimuth _bearingToLeftTip;

        protected Azimuth _bearingToRightTip;

        protected Distance _distanceToLeftTip;

        protected Distance _distanceToRightTip;

        #endregion

        public EquipmentBase(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel)
        {
            _width = width;
            _distanceFromVechile = distanceFromVechile;
            _fromDirectionOfTravel = fromDirectionOfTravel;
            _overlap = Distance.FromMeters(0);
            Position attachedPoint = new Position(new Latitude(0.0), new Longitude(0.0));
            Position centerOfEquipment = attachedPoint.TranslateTo(fromDirectionOfTravel, distanceFromVechile);
            Position leftTip = centerOfEquipment.TranslateTo(fromDirectionOfTravel.Add(180.0).Normalize().Add(-90.0), CenterToTip);
            Position rightTip = centerOfEquipment.TranslateTo(fromDirectionOfTravel.Add(180.0).Normalize().Add(90.0), CenterToTip);
            _bearingToLeftTip = attachedPoint.BearingTo(leftTip);
            _bearingToRightTip = attachedPoint.BearingTo(rightTip);
            _distanceToLeftTip = attachedPoint.DistanceTo(leftTip);
            _distanceToRightTip = attachedPoint.DistanceTo(rightTip);
        }

        public EquipmentBase(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap)
            : this(width,distanceFromVechile,fromDirectionOfTravel)
        {
            if (overlap > width.Divide(2))
                _overlap = width.Divide(2);
            else
                _overlap = overlap;
        }

        #region IEquipment Implementation

        public Distance DistanceFromVechileToCenter
        {
            get { return _distanceFromVechile; }
        }

        public Azimuth FromDirectionOfTravel
        {
            get { return _fromDirectionOfTravel; }
        }

        public Distance Width
        {
            get { return _width; }
        }

        public Distance Overlap
        {
            get { return _overlap; }
        }

        public Distance CenterToTip
        {
            get
            {
                return _width.Divide(2);
            }
        }

        public Distance WidthExclOverlap
        {
            get
            {
                return _width.Subtract(_overlap);
            }
        }

        public Distance CenterToTipWithOverlap
        {
            get { return CenterToTip.Subtract(Overlap); }
        }

        public Position GetLeftTip(Position attachedPosition, Azimuth directionOfTravel)
        {
            return attachedPosition.TranslateTo(directionOfTravel.Add(_bearingToLeftTip), _distanceToLeftTip);
        }

        public Position GetRightTip(Position attachedPosition, Azimuth directionOfTravel)
        {
            return attachedPosition.TranslateTo(directionOfTravel.Add(_bearingToRightTip), _distanceToRightTip);
        }

        public Position GetCenter(Position attachedPosition, Azimuth directionOfTravel)
        {
            return attachedPosition.TranslateTo(directionOfTravel.Add(FromDirectionOfTravel), DistanceFromVechileToCenter);
        }

        #endregion
    }
}
