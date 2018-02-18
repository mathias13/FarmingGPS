using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotSpatial.Topology;

namespace FarmingGPSLib.HelperClasses
{
    public class HelperClassAngles
    {
        public static bool AngleBetween(Angle angle, Angle left, Angle right)
        {
            if (right.DegreesPos < left.DegreesPos)
                return right.DegreesPos >= angle.DegreesPos || angle.DegreesPos >= left.DegreesPos;
            else
                return right.DegreesPos >= angle.DegreesPos && angle.DegreesPos >= left.DegreesPos;

        }
    }
}
