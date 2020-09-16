using System;
using System.Collections.Generic;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.GeometriesGraph;
using GpsUtilities.HelperClasses;


namespace FarmingGPSLib.FarmingModes
{
    public class PlowingMode : FarmingModeBase
    {

        public PlowingMode(IField field)
            : base(field)
        {
        }

        public PlowingMode(IField field, IEquipment equipment, int headLandTurns)
            : base(field, equipment, headLandTurns)
        {
        }

        public PlowingMode(IField field, IEquipment equipment, Distance headlandDistance)
            : base(field, equipment, headlandDistance)
        {

        }

        protected override void CalculateHeadLand()
        {
            double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            List<LineString> trackingLines = new List<LineString>();
            trackingLines.AddRange(GetHeadlandLines(0.0));

            foreach (LineString line in trackingLines)
                _trackingLinesHeadland.Add(new TrackingLine(line));
        }
        public override void CreateTrackingLines(Coordinate aCoord, DotSpatial.Topology.Angle direction)
        {
            base.CreateTrackingLines(aCoord, direction);
        }

        public override void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord)
        {
            base.CreateTrackingLines(aCoord, bCoord);
        }

        public override void CreateTrackingLines(TrackingLine headLine)
        {
            base.CreateTrackingLines(headLine);
            FillFieldWithTrackingLines(headLine.MainLine);
        }

        public override void CreateTrackingLines(TrackingLine trackingLine, DotSpatial.Topology.Angle directionFromLine)
        {
            this.CreateTrackingLines(trackingLine);
        }

        public override void UpdateEvents(Coordinate position, DotSpatial.Positioning.Azimuth direction)
        {
        }

        protected void FillFieldWithTrackingLines(ILineSegment baseLine)
        {
            _trackingLinesHeadland.Clear();


            double left90 = HelperClassAngles.NormalizeRadian(baseLine.Angle + HelperClassAngles.DEGREE_90_RAD);
            double right90 = HelperClassAngles.NormalizeRadian(baseLine.Angle - HelperClassAngles.DEGREE_90_RAD);
            var leftConstraints = new DotSpatial.Topology.Angle[]
            {
                new DotSpatial.Topology.Angle(HelperClassAngles.NormalizeRadian(left90 + HelperClassAngles.DEGREE_30_RAD)),
                new DotSpatial.Topology.Angle(HelperClassAngles.NormalizeRadian(right90 + HelperClassAngles.DEGREE_30_RAD))
            };
            var rightConstraints = new DotSpatial.Topology.Angle[]
            {
                new DotSpatial.Topology.Angle(HelperClassAngles.NormalizeRadian(left90 - HelperClassAngles.DEGREE_30_RAD)),
                new DotSpatial.Topology.Angle(HelperClassAngles.NormalizeRadian(right90 - HelperClassAngles.DEGREE_30_RAD))
            };
            double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            List<LineString> headlandTrackingLines = new List<LineString>();
            for (int i = 0; i < _headlandTurns; i++)
            {
                headlandTrackingLines.AddRange(GetHeadlandLines(distanceFromShell, leftConstraints, rightConstraints) );
                distanceFromShell += _equipment.WidthExclOverlap.ToMeters().Value;
            }

            foreach (LineString line in headlandTrackingLines)
                _trackingLinesHeadland.Add(new TrackingLine(line));
            //double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            //for (int i = 1; i < _headlandTurns; i++)
            //{
            //    IList<ILineString> headLandCoordinates = GetHeadLandCoordinates(distanceFromShell + 0.02);
            //    List<LineSegment> headLandLines = new List<LineSegment>();
            //    foreach (ILineString headLand in headLandCoordinates)
            //        if (HelperClassAngles.AngleBetween(new DotSpatial.Topology.Angle(headLand.Angle), angles[0], angles[1]) || HelperClassAngles.AngleBetween(new DotSpatial.Topology.Angle(headLand.Angle), angles[2], angles[3]))
            //            _trackingLinesHeadland.Add(new TrackingLine(headLand));
            //    distanceFromShell += _equipment.WidthExclOverlap.ToMeters().Value;
            //}
            //distanceFromShell += _equipment.CenterToTip.ToMeters().Value;
            ////IList<ILineString> headLandCoordinates = GetHeadLandCoordinates(distanceFromShell + 0.02); //add 2cm to make sure we dont get trackingline over headline
            ////List<LineSegment> headLandLines = new List<LineSegment>();
            ////foreach (ILineString headLand in headLandCoordinates)
            ////    headLandLines.AddRange(HelperClassLines.CreateLines(headLand.Coordinates));

            //List<Polygon> headLandToCheck = new List<Polygon>();
            //IList<ILineString> headLands = GetHeadLandCoordinates(distanceFromShell);
            //foreach (ILineString headLand in headLands)
            //    headLandToCheck.Add(new Polygon(headLand.Coordinates));

            //IEnvelope fieldEnvelope = _fieldPolygon.Envelope;
            ////Get longest projection to make sure we cover the whole field
            //ILineSegment baseLineExtended1 = new LineSegment(baseLine.Project(fieldEnvelope.TopLeft()), baseLine.Project(fieldEnvelope.BottomRight()));
            //ILineSegment baseLineExtended2 = new LineSegment(baseLine.Project(fieldEnvelope.BottomLeft()), baseLine.Project(fieldEnvelope.TopRight()));

            //ILineSegment baseLineExtended = baseLineExtended1.Length > baseLineExtended2.Length ? baseLineExtended1 : baseLineExtended2;

            //List<ILineSegment> linesExtended = new List<ILineSegment>();
            //linesExtended.Add(baseLineExtended);
            //ILineSegment extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthExclOverlap.ToMeters().Value);
            //int lineIteriator = 2;
            //while (fieldEnvelope.Intersects(extendedLine))
            //{
            //    linesExtended.Add(extendedLine);
            //    extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthExclOverlap.ToMeters().Value * lineIteriator);
            //    lineIteriator++;
            //}

            //extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthExclOverlap.ToMeters().Value);
            //lineIteriator = 2;
            //while (fieldEnvelope.Intersects(extendedLine))
            //{
            //    linesExtended.Add(extendedLine);
            //    extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthExclOverlap.ToMeters().Value * lineIteriator);
            //    lineIteriator++;
            //}

            //List<LineString> trackingLines = new List<LineString>();
            //List<Coordinate> lineCoordinates = new List<Coordinate>();
            //for (int i = 0; i < linesExtended.Count; i++)
            //{
            //    foreach (LineSegment line in headLandLines)
            //    {
            //        Coordinate intersection = line.Intersection(linesExtended[i]);
            //        if (intersection != null)
            //            lineCoordinates.Add(intersection);
            //    }

            //    for (int j = 0; j < lineCoordinates.Count; j++)
            //    {
            //        bool remove = true;
            //        foreach (Polygon polygonToCheck in headLandToCheck)
            //            if (CgAlgorithms.IsPointInRing(lineCoordinates[j], polygonToCheck.Coordinates))
            //                remove = false;
            //        if (remove)
            //        {
            //            lineCoordinates.RemoveAt(j);
            //            j--;
            //        }
            //    }

            //    if (lineCoordinates.Count == 2)
            //        trackingLines.Add(new LineString(new List<Coordinate>() { lineCoordinates[0], lineCoordinates[1] }));
            //    else
            //    {
            //        int j = 1;
            //        while (lineCoordinates.Count > 1)
            //        {
            //            if (j >= lineCoordinates.Count)
            //            {
            //                j = 1;
            //                lineCoordinates.RemoveAt(0);
            //                continue;
            //            }

            //            LineString lineToCheck = new LineString(new Coordinate[] { lineCoordinates[0], lineCoordinates[j] });
            //            foreach (Polygon polygonToCheck in headLandToCheck)
            //            {
            //                if (polygonToCheck.Contains(lineToCheck))
            //                {
            //                    trackingLines.Add(new LineString(new List<Coordinate>() { lineCoordinates[0], lineCoordinates[j] }));
            //                    lineCoordinates.RemoveAt(j);
            //                    lineCoordinates.RemoveAt(0);
            //                    j = 1;
            //                }
            //                else
            //                    j++;
            //            }

            //        }
            //    }

            //    lineCoordinates.Clear();
            //}

            //AddTrackingLines(trackingLines);
        }
    }
}
