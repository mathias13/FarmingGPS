using System;
using DotSpatial.Positioning;
using GpsUtilities.Reciever;

namespace FarmingGPSLib.Vechile
{
    public interface IVechile
    {
        Azimuth OffsetDirection { get; }

        Distance OffsetDistance { get; }

        IReceiver Receiver { get; }
    }
}
