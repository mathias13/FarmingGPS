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

        private Side _sideOfLine;

        private double _distanceTo;

        public OrientationToLine(Side sideOfLine, double distanceTo)
        {
            _sideOfLine = sideOfLine;
            _distanceTo = distanceTo;
        }

        public Side SideOfLine
        {
            get { return _sideOfLine; }
        }

        public double DistanceTo
        {
            get { return _distanceTo; }
        }
    }
}
