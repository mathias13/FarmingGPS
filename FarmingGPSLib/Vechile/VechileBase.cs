using System;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using GpsUtilities.Reciever;
using GpsUtilities.HelperClasses;
using DotSpatial.Topology.Utilities;

namespace FarmingGPSLib.Vechile
{
    public class VechileBase : IVechile
    {
        [Serializable]
        public struct VechileState
        {
            public double OffsetDirection;

            public double OffsetDistance;

            public double WheelAxesDistance;

            public double AttachPointDirection;

            public double AttachPointDistance;
        }

        protected Coordinate _position;

        protected Coordinate _attachPosition;

        protected Azimuth _direction;

        public VechileBase()
        { }

        public VechileBase(Azimuth offsetDirection, Distance offsetDistance, Distance wheelAxesDistance, Distance attachPointDistance, Azimuth attachPointDirection)
        {
            OffsetDirection = offsetDirection;
            OffsetDistance = offsetDistance;
            WheelAxesDistance = wheelAxesDistance;
            AttachPointDirection = attachPointDirection;
            AttachPointDistance = attachPointDistance;
            HasChanged = true;
        }

        public Azimuth OffsetDirection { get; private set; }

        public Distance OffsetDistance { get; private set; }

        public Distance WheelAxesDistance { get; private set; }

        public Azimuth AttachPointDirection { get; private set; }

        public Distance AttachPointDistance { get; private set; }

        #region IVechile

        public virtual Coordinate UpdatePosition(IReceiver receiver)
        {
            _position = receiver.CurrentCoordinate;
            _attachPosition = HelperClassCoordinate.ComputePoint(_position, HelperClassAngles.GetCartesianAngle(receiver.CurrentBearing - AttachPointDirection).Radians, AttachPointDistance.ToMeters().Value);
            _direction = receiver.CurrentBearing;
            return receiver.CurrentCoordinate;
        }

        public virtual Coordinate CenterRearAxle
        {
            get { return _position; }
        }

        public Coordinate AttachPoint
        {
            get { return _attachPosition; }
        }

        public virtual Azimuth VechileDirection
        {
            get { return _direction; }
        }

        public virtual bool IsReversing
        {
            get { return false; }
        }

        #endregion

        #region IStateObject

        public object StateObject
        {
            get
            {
                HasChanged = false;
                return new VechileState() { OffsetDirection = OffsetDirection.DecimalDegrees,
                    OffsetDistance = OffsetDistance.ToMeters().Value,
                    WheelAxesDistance = WheelAxesDistance.ToMeters().Value,
                    AttachPointDirection = AttachPointDirection.DecimalDegrees,
                    AttachPointDistance = AttachPointDistance.ToMeters().Value};
            }
        }

        public bool HasChanged { get; private set; } = false;

        public Type StateType
        {
            get { return typeof(VechileState); }
        }

        public virtual void RestoreObject(object restoredState)
        {
            VechileState vechileState = (VechileState)restoredState;
            OffsetDirection = new Azimuth(vechileState.OffsetDirection);
            OffsetDistance = new Distance(vechileState.OffsetDistance, DistanceUnit.Meters);
            WheelAxesDistance = new Distance(vechileState.WheelAxesDistance, DistanceUnit.Meters);
            AttachPointDirection = new Azimuth(vechileState.AttachPointDirection);
            AttachPointDistance = new Distance(vechileState.AttachPointDistance, DistanceUnit.Meters);
        }

        #endregion
    }
}
