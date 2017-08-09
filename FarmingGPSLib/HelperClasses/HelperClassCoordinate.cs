using System;
using DotSpatial.Topology;
using System.Collections.Generic;

namespace FarmingGPSLib.HelperClasses
{
    public class HelperClassCoordinate
    {
        public static bool CoordinateEqualsRoundedmm(Coordinate coord1, Coordinate coord2)
        {
            return Math.Round(coord1.X, 3) == Math.Round(coord2.X, 3) && Math.Round(coord1.Y, 3) == Math.Round(coord2.Y, 3);
        }

        public static Coordinate CoordinateRoundedmm(Coordinate coord)
        {
            return new Coordinate(Math.Round(coord.X, 3), Math.Round(coord.Y, 3));
        }

        public static Coordinate ComputePoint(Coordinate coord, double angle, double distance)
        {
            angle = (360.0 * (Math.PI / 180.0)) - angle;
            double leftX = coord.X + Math.Cos(angle) * distance;
            double leftY = coord.Y + Math.Sin(angle) * distance;
            return new Coordinate(leftX, leftY);
        }

    }

    public class CSVCoordinates
    {
        private IEnumerable<Coordinate> _coords;

        public CSVCoordinates(IEnumerable<Coordinate> coords)
        {
            _coords = coords;
        }

        public override string ToString()
        {
            string csvString = String.Empty;
            foreach (Coordinate coord in _coords)
                csvString += coord.X.ToString() + ";" + coord.Y.ToString() + Environment.NewLine;
            return csvString;
        }
    }
}
