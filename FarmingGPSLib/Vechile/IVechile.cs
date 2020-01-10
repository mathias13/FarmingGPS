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
        
        DotSpatial.Topology.Coordinate CenterRearAxle { get; }

        Azimuth VechileDirection { get; }

        DotSpatial.Topology.Coordinate UpdatePosition(IReceiver receiver);

        bool IsReversing { get; }
    }
}
