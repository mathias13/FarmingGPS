using System;
using DotSpatial.Positioning;
using GeoAPI.Geometries;
using FarmingGPSLib.FarmingModes;
using FarmingGPSLib.Vechile;
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

            public double StatStartWeight;

            public double StatEndWeight;

            public bool SideDependent;

            public double OppositeSideFromDirectionOfTravel;

            public double OppositeSideBearingToLeftTip;

            public double OppositeSideBearingToRightTip;

            public double OppositeSideDistanceToLeftTip;

            public double OppositeSideDistanceToRightTip;

            public double OffsetFromVechile;
        }

        #region Private Variables

        protected Distance _width;

        protected Distance _distanceFromVechile;

        protected Distance _overlap;

        protected Distance _offsetFromVechile;

        protected bool _sideDependent = false;

        protected bool _offsetToTheRight = false;

        protected bool _oppositeSide = false;

        protected Azimuth _fromDirectionOfTravel;

        protected Azimuth _bearingToLeftTip;

        protected Azimuth _bearingToRightTip;

        protected Distance _distanceToLeftTip;

        protected Distance _distanceToRightTip;

        protected Azimuth _oppositeSideFromDirectionOfTravel = Azimuth.Empty;

        protected Azimuth _oppositeSideBearingToLeftTip = Azimuth.Empty;

        protected Azimuth _oppositeSideBearingToRightTip = Azimuth.Empty;

        protected Distance _oppositeSideDistanceToLeftTip = Distance.Empty;

        protected Distance _oppositeSideDistanceToRightTip = Distance.Empty;

        #endregion

        public EquipmentBase()
        { }

        public EquipmentBase(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, IVechile vechile)
        {
            _width = width;
            _distanceFromVechile = distanceFromVechile;
            _fromDirectionOfTravel = fromDirectionOfTravel;
            _overlap = Distance.FromMeters(0);
            CalculateDistances();
            var vechilePos = new Position(new Latitude(1.0), new Longitude(1.0));
            var equipmentPosition = vechilePos.TranslateTo(vechile.AttachPointDirection, vechile.AttachPointDistance);
            equipmentPosition = equipmentPosition.TranslateTo(fromDirectionOfTravel, distanceFromVechile);
            var vechileLine = new Segment(vechilePos.TranslateTo(Azimuth.South, Distance.FromMeters(20.0)), vechilePos.TranslateTo(Azimuth.North, Distance.FromMeters(20.0)));
            _offsetFromVechile = vechileLine.DistanceTo(equipmentPosition);
            _offsetToTheRight = !(equipmentPosition.BearingTo(vechilePos) > Azimuth.North && equipmentPosition.BearingTo(vechilePos) < Azimuth.South);
            HasChanged = true;
        }

        public EquipmentBase(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap, IVechile vechile)
            : this(width,distanceFromVechile,fromDirectionOfTravel, vechile)
        {
            if (overlap > width.Divide(2))
                _overlap = width.Divide(2);
            else
                _overlap = overlap;
        }

        private void CalculateDistances()
        {
            var attachedPoint = new Position(new Latitude(1.0), new Longitude(1.0));
            var centerOfEquipment = attachedPoint.TranslateTo(_fromDirectionOfTravel, _distanceFromVechile);
            var leftTip = centerOfEquipment.TranslateTo(Azimuth.West, CenterToTip);
            var rightTip = centerOfEquipment.TranslateTo(Azimuth.East, CenterToTip);
            _bearingToLeftTip = attachedPoint.BearingTo(leftTip);
            _bearingToRightTip = attachedPoint.BearingTo(rightTip);
            _distanceToLeftTip = attachedPoint.DistanceTo(leftTip);
            _distanceToRightTip = attachedPoint.DistanceTo(rightTip);
            if(SideDependent)
            {
                _oppositeSideFromDirectionOfTravel = Azimuth.South.Subtract(_fromDirectionOfTravel.Subtract(Azimuth.South));
                var oppositeSideCenterOfEquipment = attachedPoint.TranslateTo(_oppositeSideFromDirectionOfTravel, _distanceFromVechile);
                var oppositeSideLeftTip = oppositeSideCenterOfEquipment.TranslateTo(Azimuth.West, CenterToTip);
                var oppositeSideRightTip = oppositeSideCenterOfEquipment.TranslateTo(Azimuth.East, CenterToTip);
                _oppositeSideBearingToLeftTip = attachedPoint.BearingTo(oppositeSideLeftTip);
                _oppositeSideBearingToRightTip = attachedPoint.BearingTo(oppositeSideRightTip);
                _oppositeSideDistanceToLeftTip = attachedPoint.DistanceTo(oppositeSideLeftTip);
                _oppositeSideDistanceToRightTip = attachedPoint.DistanceTo(oppositeSideRightTip);
            }

        }

        #region IEquipment Implementation

        public Distance DistanceFromVechileToCenter
        {
            get { return _distanceFromVechile; }
        }

        public Azimuth FromDirectionOfTravel
        {
            get
            {
                if (SideDependent && OppositeSide)
                    return _oppositeSideFromDirectionOfTravel;
                else
                    return _fromDirectionOfTravel;
            }
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

        public Distance WidthOverlap
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

        public virtual bool SideDependent
        {
            get { return _sideDependent; }
        }

        public virtual bool OffsetToTheRight
        {
            get { return _offsetToTheRight; }
            set { _offsetToTheRight = value; }
        }

        public virtual bool OppositeSide
        {
            get { return _oppositeSide; }
            set { _oppositeSide = value; }
        }

        public virtual Distance OffsetFromVechile
        {
            get { return _offsetFromVechile; }
        }
        public Coordinate GetLeftTip(Coordinate attachedPosition, Azimuth directionOfTravel)
        {
            if (SideDependent && OppositeSide)
                return HelperClassCoordinate.ComputePoint(attachedPosition, HelperClassAngles.NormalizeAzimuthHeading(directionOfTravel.Add(_oppositeSideBearingToLeftTip)).Radians, _oppositeSideDistanceToLeftTip.ToMeters().Value);
            else
                return HelperClassCoordinate.ComputePoint(attachedPosition, HelperClassAngles.NormalizeAzimuthHeading(directionOfTravel.Add(_bearingToLeftTip)).Radians, _distanceToLeftTip.ToMeters().Value);
        }

        public Coordinate GetRightTip(Coordinate attachedPosition, Azimuth directionOfTravel)
        {
            if (SideDependent && OppositeSide)
                return HelperClassCoordinate.ComputePoint(attachedPosition, HelperClassAngles.NormalizeAzimuthHeading(directionOfTravel.Add(_oppositeSideBearingToRightTip)).Radians, _oppositeSideDistanceToRightTip.ToMeters().Value);
            else
                return HelperClassCoordinate.ComputePoint(attachedPosition, HelperClassAngles.NormalizeAzimuthHeading(directionOfTravel.Add(_bearingToRightTip)).Radians, _distanceToRightTip.ToMeters().Value);
        }

        public Coordinate GetCenter(Coordinate attachedPosition, Azimuth directionOfTravel)
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
                double startWeight = 0.0;
                double endWeight = 0.0;
                if (this is IEquipmentStat)
                {
                    var stat = this as IEquipmentStat;
                    startWeight = stat.StartWeight;
                    endWeight = stat.EndWeight;
                }
                return new EquipmentState() { Width = Width.ToMeters().Value,
                    DistanceFromVechile = DistanceFromVechileToCenter.ToMeters().Value,
                    FromDirectionOfTravel = FromDirectionOfTravel.DecimalDegrees,
                    Overlap = Overlap.ToMeters().Value,
                    StatStartWeight = startWeight,
                    StatEndWeight = endWeight,
                    SideDependent = _sideDependent,
                    OppositeSideFromDirectionOfTravel = _oppositeSideFromDirectionOfTravel.DecimalDegrees,
                    OppositeSideBearingToLeftTip = _oppositeSideBearingToLeftTip.DecimalDegrees,
                    OppositeSideBearingToRightTip = _oppositeSideBearingToRightTip.DecimalDegrees,
                    OppositeSideDistanceToLeftTip = _oppositeSideDistanceToLeftTip.ToMeters().Value,
                    OppositeSideDistanceToRightTip = _oppositeSideDistanceToRightTip.ToMeters().Value,
                    OffsetFromVechile = _offsetFromVechile.ToMeters().Value};
            }
        }

        public virtual bool HasChanged { get; protected set; } = false;

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
            _sideDependent = equipmentState.SideDependent;
            _offsetFromVechile = Distance.FromMeters(equipmentState.OffsetFromVechile);
            if(SideDependent)
            {
                _oppositeSideFromDirectionOfTravel = new Azimuth(equipmentState.OppositeSideFromDirectionOfTravel);
                _oppositeSideBearingToLeftTip = new Azimuth(equipmentState.OppositeSideBearingToLeftTip);
                _oppositeSideBearingToRightTip = new Azimuth(equipmentState.OppositeSideBearingToRightTip);
                _oppositeSideDistanceToLeftTip = Distance.FromMeters(equipmentState.OppositeSideDistanceToLeftTip);
                _oppositeSideDistanceToRightTip = Distance.FromMeters(equipmentState.OppositeSideDistanceToRightTip);
            }

            if (this is IEquipmentStat)
            {
                var stat = this as IEquipmentStat;
                stat.StartWeight = equipmentState.StatStartWeight;
                stat.EndWeight = equipmentState.StatEndWeight;
            }
            CalculateDistances();
        }

        #endregion
    }
}
