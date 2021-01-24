using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmingGPSLib.FarmingModes.Tools
{
    public struct OrientationToLine
    {
        public enum Side
        {
            Left,
            Right
        }

        public OrientationToLine(Side sideOfLine, double distanceTo, bool trackingBackwards)
        {
            SideOfLine = sideOfLine;
            DistanceTo = distanceTo;
            TrackingBackwards = trackingBackwards;
        }

        public bool TrackingBackwards { get; }

        public Side SideOfLine { get; }

        public double DistanceTo { get; }
    }
}
