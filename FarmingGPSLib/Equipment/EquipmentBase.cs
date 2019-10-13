using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;

namespace FarmingGPSLib.Equipment
{
    public class EquipmentBase: IEquipment
    {
        [Serializable]
        public struct EquipmentState
        {
            public double Width;

            public double DistanceFromVechile;

            public double FromDirectionOfTravel;

            public double Overlap;

        }

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
            Position attachedPoint = new Position(new Latitude(1.0), new Longitude(1.0));
            Position centerOfEquipment = attachedPoint.TranslateTo(fromDirectionOfTravel, distanceFromVechile);
            Position leftTip = centerOfEquipment.TranslateTo(Azimuth.West, CenterToTip);
            Position rightTip = centerOfEquipment.TranslateTo(Azimuth.East, CenterToTip);
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

        public virtual Type FarmingMode
        {
            get { return null; }
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
        
        #region IStateObject

        public object StateObject
        {
            get
            {
                HasChanged = false;
                return new EquipmentState() { Width = Width.ToMeters().Value, DistanceFromVechile = DistanceFromVechileToCenter.ToMeters().Value, FromDirectionOfTravel = FromDirectionOfTravel.DecimalDegrees, Overlap = Overlap.ToMeters().Value };
            }
        }

        public bool HasChanged { get; private set; } = false;

        public Type StateType
        {
            get { return typeof(EquipmentState); }
        }

        public void RestoreObject(object restoredState)
        {
            EquipmentState equipmentState = (EquipmentState)restoredState;
            _width = Distance.FromMeters(equipmentState.Width);
            _distanceFromVechile = Distance.FromMeters(equipmentState.DistanceFromVechile);
            _fromDirectionOfTravel = new Azimuth(equipmentState.FromDirectionOfTravel);
            _overlap = Distance.FromMeters(equipmentState.Overlap);
        }

        #endregion
    }
}
