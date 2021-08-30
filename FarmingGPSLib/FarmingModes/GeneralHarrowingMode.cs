using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.GeometriesGraph;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using GpsUtilities.HelperClasses;
using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FarmingModes
{
    public class GeneralHarrowingMode : FarmingModeBase
    {
        public GeneralHarrowingMode() : base()
        { }

        public GeneralHarrowingMode(IField field)
            : base(field)
        {
            _equipment = new Harrow();
            _headlandTurns = 1;
            CalculateHeadLand();
        }

        public GeneralHarrowingMode(IField field, IEquipment equipment, int headLandTurns)
            : base(field, equipment, headLandTurns)
        {
        }

        public GeneralHarrowingMode(IField field, IEquipment equipment, Distance headlandDistance)
            : base(field, equipment, headlandDistance)
        {
        }

        protected override void CalculateHeadLand()
        {
            double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            List<LineString> trackingLines = new List<LineString>();
            for (int i = 0; i < _headlandTurns; i++)
            {
                trackingLines.AddRange(GetHeadlandLines(distanceFromShell));
                distanceFromShell += _equipment.WidthOverlap.ToMeters().Value;
            }
            foreach (LineString line in trackingLines)
                _trackingLinesHeadland.Add(new TrackingLine(line, true));
        }

        public override void CreateTrackingLines(Coordinate aCoord, DotSpatial.Topology.Angle direction)
        {
            base.CreateTrackingLines(aCoord, direction);
            FillFieldWithTrackingLines(HelperClassLines.CreateLine(aCoord, direction, 5.0));
        }

        public override void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord)
        {
            base.CreateTrackingLines(aCoord, bCoord);
            FillFieldWithTrackingLines(new LineSegment(aCoord, bCoord));
        }

        public override void CreateTrackingLines(TrackingLine headLine)
        {
            base.CreateTrackingLines(headLine);
            FillFieldWithTrackingLines(headLine.MainLine);
        }

        public override void CreateTrackingLines(TrackingLine trackingLine, DotSpatial.Topology.Angle directionFromLine)
        {
            DotSpatial.Topology.Angle newAngle = new DotSpatial.Topology.Angle(trackingLine.MainLine.Angle + directionFromLine.Radians);
            this.CreateTrackingLines(trackingLine.MainLine.P0, newAngle);
        }

        public override void UpdateEvents(ILineString positionEquipment, DotSpatial.Positioning.Azimuth direction)
        {
        }

        protected void FillFieldWithTrackingLines(ILineSegment baseLine)
        {
            double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            for (int i = 1; i < _headlandTurns; i++)
                distanceFromShell += _equipment.WidthOverlap.ToMeters().Value;
            distanceFromShell += _equipment.CenterToTip.ToMeters().Value;
            IList<ILineString> headLandCoordinates = GetHeadLandCoordinates(distanceFromShell + 0.02); //add 2cm to make sure we dont get trackingline over headland line
            List<LineSegment> headLandLines = new List<LineSegment>();
            foreach(ILineString headLand in headLandCoordinates)
                headLandLines.AddRange(HelperClassLines.CreateLines(headLand.Coordinates));

            List<Polygon> headLandToCheck = new List<Polygon>();
            IList<ILineString> headLands = GetHeadLandCoordinates(distanceFromShell);
            foreach (ILineString headLand in headLands)
                headLandToCheck.Add(new Polygon(headLand.Coordinates));

            IEnvelope fieldEnvelope = _fieldPolygon.Envelope;
            //Get longest projection to make sure we cover the whole field
            ILineSegment baseLineExtended1 = new LineSegment(baseLine.Project(fieldEnvelope.TopLeft()), baseLine.Project(fieldEnvelope.BottomRight()));
            ILineSegment baseLineExtended2 = new LineSegment(baseLine.Project(fieldEnvelope.BottomLeft()), baseLine.Project(fieldEnvelope.TopRight()));

            ILineSegment baseLineExtended = baseLineExtended1.Length > baseLineExtended2.Length ? baseLineExtended1 : baseLineExtended2;

            List<ILineSegment> linesExtended = new List<ILineSegment>();
            List<ILineSegment> linesInBetweenExtended = new List<ILineSegment>();
            linesExtended.Add(baseLineExtended);
            ILineSegment extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthOverlap.ToMeters().Value);
            int lineIteriator = 2;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Insert(0, extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthOverlap.ToMeters().Value * lineIteriator);
                lineIteriator++;
            }
            double firstOffset = _equipment.WidthOverlap.ToMeters().Value / 2.0;
            linesInBetweenExtended.Add(HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, firstOffset));
            for (int i = 1; i < lineIteriator - 1; i++)
                linesInBetweenExtended.Insert(0, HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, firstOffset + (_equipment.WidthOverlap.ToMeters().Value * i)));

            extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthOverlap.ToMeters().Value);
            lineIteriator = 2;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Add(extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthOverlap.ToMeters().Value * lineIteriator);
                lineIteriator++;
            }
            linesInBetweenExtended.Add(HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, firstOffset));
            for (int i = 1; i < lineIteriator; i++)
                linesInBetweenExtended.Add(HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, firstOffset + (_equipment.WidthOverlap.ToMeters().Value * i)));

            List<LineString> trackingLines = new List<LineString>();
            List<IGeometry> startPoints = new List<IGeometry>();
            List<IGeometry> endPoints = new List<IGeometry>();
            List<Coordinate> lineCoordinates = new List<Coordinate>();
            for (int i = 0; i < linesExtended.Count; i++)
            {
                var linesToAdd = new List<LineString>();
                foreach (LineSegment line in headLandLines)
                {
                    Coordinate intersection = line.Intersection(linesExtended[i]);
                    if (intersection != null)
                        lineCoordinates.Add(intersection);
                }

                for (int j = 0; j < lineCoordinates.Count; j++)
                {
                    bool remove = true;
                    foreach (Polygon polygonToCheck in headLandToCheck)
                        if (CgAlgorithms.IsPointInRing(lineCoordinates[j], polygonToCheck.Coordinates))
                            remove = false;
                    if (remove)
                    {
                        lineCoordinates.RemoveAt(j);
                        j--;
                    }
                }

                if (lineCoordinates.Count == 2)
                {
                    //Check angle so we dont reverse the line
                    var line = new LineSegment(lineCoordinates[0], lineCoordinates[1]);
                    if ((int)line.Angle != (int)baseLine.Angle)
                        linesToAdd.Add(new LineString(new List<Coordinate>() { line.P1, line.P0 }));
                    else
                        linesToAdd.Add(new LineString(new List<Coordinate>() { line.P0, line.P1 }));
                }
                else
                {
                    int j = 1;
                    while (lineCoordinates.Count > 1)
                    {
                        if (j >= lineCoordinates.Count)
                        {
                            j = 1;
                            lineCoordinates.RemoveAt(0);
                            continue;
                        }

                        LineString lineToCheck = new LineString(new Coordinate[] { lineCoordinates[0], lineCoordinates[j] });
                        foreach (Polygon polygonToCheck in headLandToCheck)
                        {
                            if (polygonToCheck.Contains(lineToCheck))
                            {
                                //Check angle so we dont reverse the line
                                var line = new LineSegment(lineCoordinates[0], lineCoordinates[j]);
                                if ((int)line.Angle != (int)baseLine.Angle)
                                    linesToAdd.Add(new LineString(new List<Coordinate>() { line.P1, line.P0 }));
                                else
                                    linesToAdd.Add(new LineString(new List<Coordinate>() { line.P0, line.P1 }));
                                lineCoordinates.RemoveAt(j);
                                lineCoordinates.RemoveAt(0);
                                j = 1;
                            }
                            else
                                j++;
                        }

                    }
                }

                if(linesToAdd.Count > 0)
                {
                    var boxCoords = new Coordinate[] { linesInBetweenExtended[i + 1].P0, linesInBetweenExtended[i + 1].P1, linesInBetweenExtended[i].P1, linesInBetweenExtended[i].P0, linesInBetweenExtended[i + 1].P0 };
                    var box = new Polygon(boxCoords);
                    var geometries = new List<IGeometry>();
                    foreach (var headland in headLandToCheck)
                        geometries.Add(box.Intersection(headland));

                    var lines = new List<LineString>();
                    var coords = new List<Coordinate>();
                    var angleToCompare = Math.Round(baseLineExtended.Angle, 3);
                    foreach (var geometry in geometries)
                    {
                        for (int j = 0; j < geometry.Coordinates.Count; j++)
                        {
                            if (j == geometry.Coordinates.Count - 1)
                            {
                                if (coords.Count > 0)
                                {
                                    coords.Add(geometry.Coordinates[j]);
                                    lines.Add(new LineString(coords.ToArray()));
                                }
                            }
                            else
                            {
                                var segment1 = new LineSegment(geometry.Coordinates[j], geometry.Coordinates[j + 1]);
                                var segment2 = new LineSegment(geometry.Coordinates[j + 1], geometry.Coordinates[j]);
                                if (Math.Round(segment1.Angle, 3) == angleToCompare || Math.Round(segment2.Angle, 3) == angleToCompare)
                                {
                                    if (coords.Count > 0)
                                    {
                                        coords.Add(geometry.Coordinates[j]);
                                        lines.Add(new LineString(coords.ToArray()));
                                    }
                                    coords.Clear();
                                }
                                else
                                    coords.Add(geometry.Coordinates[j]);
                            }
                        }
                    }

                    foreach (var lineToAdd in linesToAdd)
                    {
                        trackingLines.Add(lineToAdd);
                        IGeometry startPoint = lineToAdd.StartPoint;
                        IGeometry endPoint = lineToAdd.EndPoint;
                        foreach(var line in lines)
                        {
                            var distance1 = lineToAdd.StartPoint.Distance(line);
                            var distance2 = lineToAdd.EndPoint.Distance(line);
                            if (line.IsWithinDistance(lineToAdd.StartPoint, 0.1))
                                startPoint = line;
                            else if (line.IsWithinDistance(lineToAdd.EndPoint, 0.1))
                                endPoint = line;
                        }
                        startPoints.Add(startPoint);
                        endPoints.Add(endPoint);
                    }
                }

                lineCoordinates.Clear();
            }

            AddTrackingLines(trackingLines, startPoints, endPoints);
        }
    
        protected virtual void AddTrackingLines(IList<LineString> trackingLines, IList<IGeometry> startPoints, IList<IGeometry> endPoints)
        {
            _trackingLines.Clear();
            for (int i = 0; i < trackingLines.Count; i++)
                _trackingLines.Add(new TrackingLine(trackingLines[i], false));
        }
    }
}
