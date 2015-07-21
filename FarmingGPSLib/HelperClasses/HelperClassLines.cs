using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using DotSpatial.Topology.GeometriesGraph;

namespace FarmingGPSLib.HelperClasses
{
    public class HelperClassLines
    {
        public static IList<LineSegment> CreateLines(IList<Coordinate> coordinates)
        {
            List<LineSegment> lines = new List<LineSegment>();
            for (int i = 0; i < coordinates.Count - 1; i++)
                lines.Add(new LineSegment(coordinates[i], coordinates[i + 1]));
            return lines;
        }

        public static ILineSegment ComputeOffsetSegment(ILineSegment lineSegment, PositionType side, double distance)
        {
            int sideSign = side == PositionType.Left ? 1 : -1;
            double dx = lineSegment.P1.X - lineSegment.P0.X;
            double dy = lineSegment.P1.Y - lineSegment.P0.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            // u is the vector that is the length of the offset, in the direction of the segment
            double ux = sideSign * distance * dx / len;
            double uy = sideSign * distance * dy / len;
            return new LineSegment(new Coordinate(lineSegment.P0.X - uy, lineSegment.P0.Y + ux), new Coordinate(lineSegment.P1.X - uy, lineSegment.P1.Y + ux));
        }

        public static Coordinate Midpoint(ILineSegment lineSegment)
        {
            return new Coordinate((lineSegment.P0.X + lineSegment.P1.X) / 2, (lineSegment.P0.Y + lineSegment.P1.Y) / 2);
        }

        public static ILineSegment CreateLine(Coordinate startCoord, Angle angle, double distance)
        {    
            double x = startCoord.X + (distance * Angle.Cos(angle));
            double y = startCoord.Y + (distance * Angle.Sin(angle));
            return new LineSegment(startCoord, new Coordinate(x, y));
        }
        
    }
}
