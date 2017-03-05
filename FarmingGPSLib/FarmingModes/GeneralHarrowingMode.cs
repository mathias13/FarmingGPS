using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.GeometriesGraph;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.HelperClasses;
using DotSpatial.Positioning;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FarmingGPSLib.FarmingModes
{
    public class GeneralHarrowingMode : FarmingModeBase, ITrackingLineModes
    {
        protected IEquipment _equipment;

        protected int _headlandTurns;

        public GeneralHarrowingMode(IField field)
            : base(field)
        {
            _equipment = new Harrow(Distance.FromMeters(6), Distance.FromMeters(1), new Azimuth(180.0));
            _headlandTurns = 1;
            CalculateHeadLand();
        }

        public GeneralHarrowingMode(IField field, IEquipment equipment, int headLandTurns)
            : base(field)
        {
            _equipment = equipment;
            _headlandTurns = headLandTurns;
            CalculateHeadLand();
        }

        protected void CalculateHeadLand()
        {
            double distanceFromShell = _equipment.CenterOfWidth.ToMeters().Value;
            List<LineString> trackingLines = new List<LineString>();
            for (int i = 0; i < _headlandTurns; i++)
            {
                trackingLines.AddRange(GetHeadlandAround(distanceFromShell));
                distanceFromShell += _equipment.CenterToCenter.ToMeters().Value;
            }
            foreach (LineString line in trackingLines)
                _trackingLinesHeadLand.Add(new TrackingLine(line));
        }

        public void CreateTrackingLines(Coordinate aCoord, DotSpatial.Topology.Angle direction)
        {
            direction.DegreesPos += 90;
            direction.Radians = direction.Radians * -1;
            FillFieldWithTrackingLines(HelperClassLines.CreateLine(aCoord, direction, 5.0));
        }

        public void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord)
        {
            FillFieldWithTrackingLines(new LineSegment(aCoord, bCoord));
        }

        public void CreateTrackingLines(TrackingLine headLine)
        {
            IList<LineSegment> lines = HelperClassLines.CreateLines(headLine.Points);
            LineSegment baseLine = lines[0];
            for (int i = 1; i < lines.Count; i++)
                if (lines[i].Length > baseLine.Length)
                    baseLine = lines[i];

            FillFieldWithTrackingLines(baseLine);
        }

        protected void FillFieldWithTrackingLines(ILineSegment baseLine)
        {
            double distanceFromShell = _equipment.CenterOfWidth.ToMeters().Value;
            for (int i = 1; i < _headlandTurns; i++)
                distanceFromShell += _equipment.CenterToCenter.ToMeters().Value;
            distanceFromShell += _equipment.CenterOfWidth.ToMeters().Value + 0.02; //add 2cm to make sure we dont get trackingline over headline
            IList<Coordinate> headLandCoordinates = GetHeadlandAroundPoints(distanceFromShell);
            IList<LineSegment> headLandLines = HelperClassLines.CreateLines(headLandCoordinates);

            IEnvelope fieldEnvelope = _fieldPolygon.Envelope;
            //Get longest projection to make sure we cover the whole field
            ILineSegment baseLineExtended1 = new LineSegment(baseLine.Project(fieldEnvelope.TopLeft()), baseLine.Project(fieldEnvelope.BottomRight()));
            ILineSegment baseLineExtended2 = new LineSegment(baseLine.Project(fieldEnvelope.BottomLeft()), baseLine.Project(fieldEnvelope.TopRight()));

            ILineSegment baseLineExtended = baseLineExtended1.Length > baseLineExtended2.Length ? baseLineExtended1 : baseLineExtended2;

            List<ILineSegment> linesExtended = new List<ILineSegment>();
            linesExtended.Add(baseLineExtended);
            ILineSegment extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.CenterToCenter.ToMeters().Value);
            int lineIteriator = 1;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Add(extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.CenterToCenter.ToMeters().Value * lineIteriator);
                lineIteriator++;
            }

            extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.CenterToCenter.ToMeters().Value);
            lineIteriator = 1;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Add(extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.CenterToCenter.ToMeters().Value * lineIteriator);
                lineIteriator++;
            }

            List<LineString> trackingLines = new List<LineString>();
            List<Coordinate> lineCoordinates = new List<Coordinate>();
            for (int i = 0; i < linesExtended.Count; i++)
            {
                foreach (LineSegment line in headLandLines)
                {
                    Coordinate intersection = line.Intersection(linesExtended[i]);
                    if (intersection != null)
                        lineCoordinates.Add(intersection);
                }
                while (lineCoordinates.Count > 1)
                {
                    bool firstPointUsed = false;
                    for (int j = 1; j < lineCoordinates.Count; j++)
                    {
                        if (CgAlgorithms.IsPointInRing(HelperClassLines.Midpoint(new LineSegment(lineCoordinates[0], lineCoordinates[j])), headLandCoordinates))
                        {
                            if (lineCoordinates.Count > 2)
                            {
                                LineSegment lineToCheck = new LineSegment(lineCoordinates[0], lineCoordinates[j]);
                                bool stillIntersect = false;
                                foreach (LineSegment line in headLandLines)
                                {
                                    Coordinate intersection = line.Intersection(lineToCheck);
                                    if (intersection == null)
                                        continue;
                                    if (HelperClassCoordinate.CoordinateEqualsRoundedmm(intersection, lineToCheck.P0) ||
                                        HelperClassCoordinate.CoordinateEqualsRoundedmm(intersection, lineToCheck.P1))
                                        continue;
                                    if (intersection != null)
                                    {
                                        stillIntersect = true;
                                        break;
                                    }
                                }
                                if (stillIntersect)
                                    continue;
                            }
                            trackingLines.Add(new LineString(new List<Coordinate>() { lineCoordinates[0], lineCoordinates[j] }));
                            lineCoordinates.RemoveAt(j);
                            lineCoordinates.RemoveAt(0);
                            firstPointUsed = true;
                        }
                    }
                    if (!firstPointUsed)
                        lineCoordinates.RemoveAt(0);
                }

                lineCoordinates.Clear();
            }

            _trackingLines.Clear();
            foreach (LineString line in trackingLines)
                _trackingLines.Add(new TrackingLine(line));
        }
    
    }
}
