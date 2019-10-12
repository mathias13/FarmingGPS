using System;
using DotSpatial.Positioning;
using GpsUtilities.Reciever;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.Vechile
{
    public class VechileBase : IVechile, IStateObject
    {
        [Serializable]
        public struct VechileState
        {
            public double OffsetDirection;

            public double OffsetDistance;
        }

        public VechileBase(Azimuth offsetDirection, Distance offsetDistance, IReceiver receiver)
        {
            OffsetDirection = offsetDirection;
            OffsetDistance = offsetDistance;
            Receiver = receiver;
            HasChanged = true;
        }

        public Azimuth OffsetDirection { get; private set; }

        public Distance OffsetDistance { get; private set; }

        public IReceiver Receiver { get; private set; }

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
    }
}
