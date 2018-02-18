using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using FarmingGPSLib.FarmingModes.Tools;

namespace FarmingGPSLib.FarmingModes
{
    public interface IFarmingMode
    {
        void CreateTrackingLines(Coordinate aCoord, Angle direction);

        void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord);

        void CreateTrackingLines(TrackingLine headLine);

        void UpdateEvents(Coordinate position, DotSpatial.Positioning.Azimuth direction);

        IList<TrackingLine> TrackingLinesHeadLand
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
