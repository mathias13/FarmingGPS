using System;
using DotSpatial.Topology;
using DotSpatial.Positioning;

namespace FarmingGPSLib.FarmingModes.Tools
{
    public interface IFarmingEvent
    {
        string Message
        { 
            get; 
        }

        bool EventFired(Azimuth directionOfTravel, Coordinate coord);

        bool EventFired(Azimuth directionOfTravel, ILineString equipmentCoords);
    }
}
