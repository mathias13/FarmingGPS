using System;
using DotSpatial.Positioning;
using GpsUtilities.Reciever;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.Vechile
{
    public interface IVechile: IStateObject
    {
        Azimuth OffsetDirection { get; }

        Distance OffsetDistance { get; }

        Distance WheelAxesDistance { get; }

        Azimuth AttachPointDirection { get; }

        Distance AttachPointDistance { get; }

        DotSpatial.Topology.Coordinate CenterRearAxle { get; }

        DotSpatial.Topology.Coordinate AttachPoint { get; }

        Azimuth VechileDirection { get; }

        DotSpatial.Topology.Coordinate UpdatePosition(IReceiver receiver);

        bool IsReversing { get; }
    }
}
