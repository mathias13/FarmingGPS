using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;

namespace FarmingGPSLib.Equipment
{
    public class Plow : EquipmentBase
    {
        public Plow():base()
        {
        }

        public Plow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel)
            : base(width, distanceFromVechile, fromDirectionOfTravel)
        {
        }

        public Plow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap)
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
