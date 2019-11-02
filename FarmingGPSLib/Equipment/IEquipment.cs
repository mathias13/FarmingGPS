using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.Equipment
{
    public interface IEquipment
    {
        Distance DistanceFromVechileToCenter
        {
            get;
        }

        Azimuth FromDirectionOfTravel
        {
            get;
        }

        Distance Width
        {
            get;
        }

        Distance Overlap
        {
            get;
        }

        Distance CenterToTip
        {
            get;
        }

        Distance WidthExclOverlap
        {
            get;
        }

        Distance CenterToTipWithOverlap
        {
            get;
        }
        
        Type FarmingMode
        {
            get;
        }

        DotSpatial.Topology.Coordinate GetLeftTip(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel);

        DotSpatial.Topology.Coordinate GetRightTip(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel);

        DotSpatial.Topology.Coordinate GetCenter(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel);
    }
}
