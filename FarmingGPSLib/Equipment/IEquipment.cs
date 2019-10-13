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

        Position GetLeftTip(Position attachedPosition, Azimuth directionOfTravel);

        Position GetRightTip(Position attachedPosition, Azimuth directionOfTravel);

        Position GetCenter(Position attachedPosition, Azimuth directionOfTravel);
    }
}
