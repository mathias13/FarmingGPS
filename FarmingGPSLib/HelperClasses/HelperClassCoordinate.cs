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
            double leftX = coord.X + Math.Cos(angle) * distance;
            double leftY = coord.Y + Math.Sin(angle) * distance;
            return new Coordinate(leftX, leftY);
        }

        public static Coordinate FindClosestCoordinate(IList<Coordinate> coordinates, Coordinate coordinate)
        {
            double prevDistance = double.MaxValue;
            Coordinate coordinateToReturn = Coordinate.Empty;
            foreach (Coordinate coordinateItem in coordinates)
            {
                double distance = coordinate.Distance(coordinateItem);
                if (prevDistance > distance)
                {
                    coordinateToReturn = coordinateItem;
                    prevDistance = distance;
                }
            }
            return coordinateToReturn;
        }
    }
}
