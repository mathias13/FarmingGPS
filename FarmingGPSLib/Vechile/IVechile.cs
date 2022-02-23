using DotSpatial.Positioning;
using FarmingGPSLib.StateRecovery;
using GeoAPI.Geometries;
using GpsUtilities.Reciever;

namespace FarmingGPSLib.Vechile
{
    public interface IVechile: IStateObject
    {
        Azimuth OffsetDirection { get; }

        Distance OffsetDistance { get; }

        Distance WheelAxesDistance { get; }

        Azimuth AttachPointDirection { get; }

        Distance AttachPointDistance { get; }

        Coordinate CenterRearAxle { get; }

        Coordinate AttachPoint { get; }

        Azimuth VechileDirection { get; }

        Coordinate UpdatePosition(IReceiver receiver);

        bool IsReversing { get; }
    }
}
