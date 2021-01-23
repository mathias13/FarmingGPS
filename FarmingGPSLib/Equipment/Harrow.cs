using System;
using DotSpatial.Positioning;
using FarmingGPSLib.FarmingModes;
using FarmingGPSLib.Vechile;

namespace FarmingGPSLib.Equipment
{
    public class Harrow : EquipmentBase
    {
        public Harrow():base()
        {
        }

        public Harrow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, vechile)
        {
        }

        public Harrow(Distance width, Distance distanceFromVechile, Azimuth fromDirectionOfTravel, Distance overlap, IVechile vechile)
            : base(width, distanceFromVechile, fromDirectionOfTravel, overlap, vechile)
        {
        }

        public override Type FarmingMode
        {
            get { return typeof(GeneralHarrowingMode); }
        }
    }
}
