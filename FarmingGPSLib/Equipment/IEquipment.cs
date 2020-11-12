using System;
using DotSpatial.Positioning;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.Equipment
{
    public interface IEquipment : IStateObject
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
            set;
        }

        Distance CenterToTip
        {
            get;
        }

        Distance WidthOverlap
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

        bool SideDependent
        {
            get;
        }

        DotSpatial.Topology.Coordinate GetLeftTip(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel);

        DotSpatial.Topology.Coordinate GetRightTip(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel);

        DotSpatial.Topology.Coordinate GetCenter(DotSpatial.Topology.Coordinate attachedPosition, Azimuth directionOfTravel);
    }
}
