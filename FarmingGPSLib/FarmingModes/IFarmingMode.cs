using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.FarmingModes
{
    public interface IFarmingMode : IStateObject
    {
        void CreateTrackingLines(Coordinate aCoord, Angle direction);

        void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord);

        void CreateTrackingLines(TrackingLine headLine);

        void UpdateEvents(Coordinate position, DotSpatial.Positioning.Azimuth direction);

        IList<TrackingLine> TrackingLinesHeadland
        {
            get;
        }

        IList<TrackingLine> TrackingLines
        {
            get;
        }

        TrackingLine GetClosestLine(Coordinate position);

        event EventHandler<string> FarmingEvent;
    }
}
