using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;
using GpsUtilities.HelperClasses;

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

        public EquipmentBase()
        { }

        public EquipmentBase(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel)
        {
            _width = width;
            _distanceFromVechile = distanceFromVechile;
            _fromDirectionOfTravel = fromDirectionOfTravel;
            _overlap = Distance.FromMeters(0);
            CalculateDistances();
            HasChanged = true;
        }

        public EquipmentBase(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap)
            : this(width,distanceFromVechile,fromDirectionOfTravel)
        {
            if (overlap > width.Divide(2))
                _overlap = width.Divide(2);
            else
                _overlap = overlap;
        }

        private void CalculateDistances()
        {
            Position attachedPoint = new Position(new Latitude(1.0), new Longitude(1.0));
            Position centerOfEquipment = attachedPoint.TranslateTo(_fromDirectionOfTravel, _distanceFromVechile);
            Position leftTip = centerOfEquipment.TranslateTo(Azimuth.West, CenterToTip);
            Position rightTip = centerOfEquipment.TranslateTo(Azimuth.East, CenterToTip);
            _bearingToLeftTip = attachedPoint.BearingTo(leftTip);
            _bearingToRightTip = attachedPoint.BearingTo(rightTip);
            _distanceToLeftTip = attachedPoint.DistanceTo(leftTip);
            _distanceToRightTip = attachedPoint.DistanceTo(rightTip);
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
            set { _overlap = value; }
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

        public DotSpatial.Topology.Coordinate GetLeftTip(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel)
        {
            return HelperClassCoordinate.ComputePoint(attachedPosition, HelperClassAngles.NormalizeAzimuthHeading(directionOfTravel.Add(_bearingToLeftTip)).Radians, _distanceToLeftTip.ToMeters().Value);
        }

        public DotSpatial.Topology.Coordinate GetRightTip(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel)
        {
            return HelperClassCoordinate.ComputePoint(attachedPosition, HelperClassAngles.NormalizeAzimuthHeading(directionOfTravel.Add(_bearingToRightTip)).Radians, _distanceToRightTip.ToMeters().Value);
        }

        public DotSpatial.Topology.Coordinate GetCenter(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel)
        {
            return HelperClassCoordinate.ComputePoint(attachedPosition, HelperClassAngles.NormalizeAzimuthHeading(directionOfTravel.Add(FromDirectionOfTravel)).Radians, DistanceFromVechileToCenter.ToMeters().Value);
        }

        #endregion
        
        #region IStateObject

        public virtual object StateObject
        {
            get
            {
                HasChanged = false;
                return new EquipmentState() { Width = Width.ToMeters().Value, DistanceFromVechile = DistanceFromVechileToCenter.ToMeters().Value, FromDirectionOfTravel = FromDirectionOfTravel.DecimalDegrees, Overlap = Overlap.ToMeters().Value };
            }
        }

        public virtual bool HasChanged { get; private set; } = false;

        public virtual Type StateType
        {
            get { return typeof(EquipmentState); }
        }

        public virtual void RestoreObject(object restoredState)
        {
            EquipmentState equipmentState = (EquipmentState)restoredState;
            _width = Distance.FromMeters(equipmentState.Width);
            _distanceFromVechile = Distance.FromMeters(equipmentState.DistanceFromVechile);
            _fromDirectionOfTravel = new Azimuth(equipmentState.FromDirectionOfTravel);
            _overlap = Distance.FromMeters(equipmentState.Overlap);
            CalculateDistances();
        }

        #endregion
    }
}
