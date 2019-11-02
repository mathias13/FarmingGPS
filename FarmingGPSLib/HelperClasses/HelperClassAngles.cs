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
            double angleRadian = NormalizeRadian(angle.Radians);
            double leftRadian = NormalizeRadian(left.Radians);
            double rightRadian = NormalizeRadian(right.Radians);

            if (rightRadian > leftRadian)
                return rightRadian <= angleRadian || angleRadian <= leftRadian;
            else
                return rightRadian <= angleRadian && angleRadian <= leftRadian;
            //if (right.DegreesPos < left.DegreesPos)
            //    return right.DegreesPos >= angle.DegreesPos || angle.DegreesPos >= left.DegreesPos;
            //else
            //    return right.DegreesPos >= angle.DegreesPos && angle.DegreesPos >= left.DegreesPos;

        }

        public static Angle NormalizeAzimuthHeading(DotSpatial.Positioning.Azimuth azimuth)
        {
            Angle angle = new Angle();
            angle.DegreesPos = azimuth.Subtract(90.0).Normalize().DecimalDegrees;
            return angle;
        }

        private static double NormalizeRadian(double radian)
        {
            double newRadian = radian;
            while (newRadian > Math.PI)
                newRadian -= Math.PI * 2.0;
            while (newRadian < Math.PI * -1.0)
                newRadian += Math.PI * 2.0;
            return newRadian;
        }
    }
}
