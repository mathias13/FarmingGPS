using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;
using FarmingGPSLib.Vechile;

namespace FarmingGPSLib.Equipment
{
    public class Plow : EquipmentBase
    {
        public Plow():base()
        {
        }

        public Plow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, vechile)
        {
        }

        public Plow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap, vechile)
        {
        }

        public override Type FarmingMode
        {
            get { return typeof(PlowingMode); }
        }

        public override bool SideDependent
        {
            get { return true; }
        }
    }
}
