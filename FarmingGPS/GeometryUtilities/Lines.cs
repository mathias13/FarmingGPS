using System;
using DotSpatial.Topology;


namespace FarmingGPS.GeometryUtilities
{
    public static class Lines
    {
        /*public static bool LinesIntersect(ILineSegment line1, ILineSegment line2)
        {
            double A1 = line1.P1.Y - line1.P0.Y;
            double A2 = line2.P1.Y - line2.P0.Y;
            double B1 = line1.P0.X - line1.P1.X;
            double B2 = line2.P0.X - line2.P1.X;
            double C1 = A1 * line1.P0.X + B1 * line1.P0.Y;
            double C2 = A2 * line2.P0.X + B2 * line2.P0.Y;
            double det = A1 * B2 - A2 * B1;

            if(det == 0)
                return true;
            else
            {
                double x = (B2 * C1 - B1 * C2) / det;
                double y = (A1 * C2 - A2 * C1) / det;
            }

        }*/
    }
}
