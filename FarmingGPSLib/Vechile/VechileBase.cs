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
        }

        private IReceiver _receiver;

        public VechileBase()
        { }

        public VechileBase(Azimuth offsetDirection, Distance offsetDistance, IReceiver receiver)
        {
            OffsetDirection = offsetDirection;
            OffsetDistance = offsetDistance;
            Receiver = receiver;
            HasChanged = true;
        }

        public Azimuth OffsetDirection { get; private set; }

        public Distance OffsetDistance { get; private set; }

        public IReceiver Receiver
        {
            get
            {
                return _receiver;
            }
            set
            {
                _receiver = value;
                _receiver.OffsetDirection = OffsetDirection;
                _receiver.OffsetDistance = OffsetDistance;
            }
        }

        #region IStateObject

        public object StateObject
        {
            get
            {
                HasChanged = false;
                return new VechileState() { OffsetDirection = OffsetDirection.DecimalDegrees, OffsetDistance = OffsetDistance.ToMeters().Value };
            }
        }

        public bool HasChanged { get; private set; } = false;

        public Type StateType
        {
            get { return typeof(VechileState); }
        }

        public void RestoreObject(object restoredState)
        {
            VechileState vechileState = (VechileState)restoredState;
            OffsetDirection = new Azimuth(vechileState.OffsetDirection);
            OffsetDistance = new Distance(vechileState.OffsetDistance, DistanceUnit.Meters);
        }

        #endregion
    }
}
