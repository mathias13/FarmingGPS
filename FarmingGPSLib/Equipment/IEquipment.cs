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

        Position GetLeftTip(Position attachedPosition, Azimuth directionOfTravel);

        Position GetRightTip(Position attachedPosition, Azimuth directionOfTravel);

        Position GetCenter(Position attachedPosition, Azimuth directionOfTravel);
    }
}
