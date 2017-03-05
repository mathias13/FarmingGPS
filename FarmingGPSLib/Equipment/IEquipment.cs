using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.Equipment
{
    public interface IEquipment
    {
        Distance DistanceFromVechile
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

        Distance CenterOfWidth
        {
            get;
        }

        Distance CenterToCenter
        {
            get;
        }

        Distance CenterOfWidthWithOverlap
        {
            get;
        }

        Position GetLeftTip(Position attachedPosition, Azimuth directionOfTravel);

        Position GetRightTip(Position attachedPosition, Azimuth directionOfTravel);

        Position GetCenter(Position attachedPosition, Azimuth directionOfTravel);
    }
}
