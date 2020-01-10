using System;
using DotSpatial.Positioning;
using GpsUtilities.Reciever;

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
        }

        protected DotSpatial.Topology.Coordinate _position;

        protected Azimuth _direction;

        public VechileBase()
        { }

        public VechileBase(Azimuth offsetDirection, Distance offsetDistance, Distance wheelAxesDistance)
        {
            OffsetDirection = offsetDirection;
            OffsetDistance = offsetDistance;
            WheelAxesDistance = wheelAxesDistance;
            HasChanged = true;
        }

        public Azimuth OffsetDirection { get; private set; }

        public Distance OffsetDistance { get; private set; }

        public Distance WheelAxesDistance { get; private set; }

        #region IVechile

        public virtual DotSpatial.Topology.Coordinate UpdatePosition(IReceiver receiver)
        {
            _position = receiver.CurrentCoordinate;
            _direction = receiver.CurrentBearing;
            return receiver.CurrentCoordinate;
        }

        public virtual DotSpatial.Topology.Coordinate CenterRearAxle
        {
            get { return _position; }
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
                return new VechileState() { OffsetDirection = OffsetDirection.DecimalDegrees, OffsetDistance = OffsetDistance.ToMeters().Value, WheelAxesDistance = WheelAxesDistance.ToMeters().Value };
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
        }

        #endregion
    }
}
