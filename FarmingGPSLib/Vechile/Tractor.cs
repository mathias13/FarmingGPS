using System;
using DotSpatial.Positioning;
using GpsUtilities.Reciever;
using FarmingGPSLib.StateRecovery;

namespace FarmingGPSLib.Vechile
{
    public class Tractor : VechileBase
    {
        public Tractor() : base()
        { }

        public Tractor(Azimuth offsetDirection, Distance offsetDistance, IReceiver receiver) : base(offsetDirection,offsetDistance,receiver)
        { }
    }
}
