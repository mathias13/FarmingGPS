using System;
using DotSpatial.Topology;

namespace FarmingGPSLib.FarmingModes.Tools
{
    interface IFarmingEvent
    {
        string Message
        { 
            get; 
        }

        bool EventFired(Angle angle, Coordinate coord);

    }
}
