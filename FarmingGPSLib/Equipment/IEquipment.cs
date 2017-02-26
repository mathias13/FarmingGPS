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

        Angle FromDirectionOfTravel
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
    }
}
