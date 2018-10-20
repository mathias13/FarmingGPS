using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;

namespace FarmingGPSLib.Equipment
{
    public class Harrow : EquipmentBase
    {
        public Harrow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel)
            : base(width, distanceFromVechile, fromDirectionOfTravel)
        {
        }

        public Harrow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap)
        {
        }

        public override Type FarmingMode
        {
            get { return typeof(GeneralHarrowingMode); }
        }
    }
}
