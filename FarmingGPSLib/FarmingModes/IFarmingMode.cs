using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using FarmingGPSLib.FarmingModes.Tools;

namespace FarmingGPSLib.FarmingModes
{
    public interface IFarmingMode
    {
        IList<TrackingLine> TrackingLinesHeadLand
        {
            get;
        }

        IList<TrackingLine> TrackingLines
        {
            get;
        }

        TrackingLine GetClosestLine();
    }
}
