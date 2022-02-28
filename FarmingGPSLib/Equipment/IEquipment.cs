using System;
using DotSpatial.Positioning;
using GeoAPI.Geometries;
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

        bool OppositeSide
        {
            get;
            set;
        }
        bool OffsetToTheRight
        {
            get;
            set;
        }

        Distance OffsetFromVechile
        {
            get;
        }

        Coordinate GetLeftTip(Coordinate attachedPosition, Azimuth directionOfTravel);

        Coordinate GetRightTip(Coordinate attachedPosition, Azimuth directionOfTravel);

        Coordinate GetCenter(Coordinate attachedPosition, Azimuth directionOfTravel);
    }
}
