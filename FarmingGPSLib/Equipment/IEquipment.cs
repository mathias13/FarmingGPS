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
    }
}
