using DotSpatial.Topology;
using FarmingGPSLib.FarmingModes.Tools;
using System;

namespace FarmingGPSLib.FarmingModes
{
    interface ITrackingLineModes
    {
        void CreateTrackingLines(Coordinate aCoord, Angle direction);

        void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord);

        void CreateTrackingLines(TrackingLine headLine);
    }
}
