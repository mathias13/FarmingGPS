using System;
using System.Collections.Generic;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.GeometriesGraph;
using GpsUtilities.HelperClasses;
using DotSpatial.Topology.Utilities;
using DotSpatial.Projections.Transforms;

namespace FarmingGPSLib.FarmingModes
{
    public class PlowingMode : FarmingModeBase
    {
        [Serializable]
        public struct PlowingModeState
        {
            public List<SimpleLine> TrackingLines;

            public List<SimpleLine> TrackingLinesHeadLand;

            public double StartDistance;

            public double StopDistance;

            public PlowingModeState(List<SimpleLine> trackingLines, List<SimpleLine> trackingLinesHeadLand, double startDistance, double stopDistance)
            {
                TrackingLines = trackingLines;
                TrackingLinesHeadLand = trackingLinesHeadLand;
                StartDistance = startDistance;
                StopDistance = stopDistance;
            }
        }

        private double _startDistance = -4.0;

        private double _stopDistance = 0.0;

        public PlowingMode() : base()
        {
        }

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

        public override TrackingLine GetClosestLine(Coordinate position, Azimuth direction)
        {
            var directionForward = new DotSpatial.Topology.Angle((Azimuth.Maximum - direction.Subtract(90.0).DecimalDegrees).ToRadians().Value);
            double distanceToLine = double.MaxValue;
            bool inDirection = false;
            TrackingLine closestLine = null;
            foreach (TrackingLine trackingLine in _trackingLines)
            {
                if (trackingLine.Depleted)
                    continue;

                double tempDistance = trackingLine.GetDistanceToLine(position);
                if (tempDistance < distanceToLine)
                {
                    inDirection = HelperClassAngles.AngleBetween(directionForward, new DotSpatial.Topology.Angle(trackingLine.MainLine.Angle + HelperClassAngles.DEGREE_30_RAD), new DotSpatial.Topology.Angle(trackingLine.MainLine.Angle - HelperClassAngles.DEGREE_30_RAD)) ||
                        HelperClassAngles.AngleBetween(directionForward, new DotSpatial.Topology.Angle(trackingLine.MainLine.Angle + HelperClassAngles.DEGREE_180_RAD + HelperClassAngles.DEGREE_30_RAD), new DotSpatial.Topology.Angle(trackingLine.MainLine.Angle + HelperClassAngles.DEGREE_180_RAD - HelperClassAngles.DEGREE_30_RAD));
                    distanceToLine = tempDistance;
                    closestLine = trackingLine;
                }
            }

            if (!(inDirection && distanceToLine < 1.5))
            {
                foreach (TrackingLine trackingLine in _trackingLinesHeadland)
                {
                    if (trackingLine.Depleted)
                        continue;

                    double tempDistance = trackingLine.GetDistanceToLine(position);
                    if (tempDistance < distanceToLine)
                    {
                        distanceToLine = tempDistance;
                        closestLine = trackingLine;
                    }
                }
            }

            return closestLine;
        }

        protected override void CalculateHeadLand()
        {
            double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            List<LineString> trackingLines = new List<LineString>();
            trackingLines.AddRange(GetHeadlandLines(distanceFromShell));

            foreach (LineString line in trackingLines)
                _trackingLinesHeadland.Add(new TrackingLine(line, false));
        }

        public override void CreateTrackingLines(Coordinate aCoord, DotSpatial.Topology.Angle direction)
        {
            base.CreateTrackingLines(aCoord, direction);
            FillFieldWithTrackingLines(HelperClassLines.CreateLine(aCoord, direction, 5.0), 0.0);
        }

        public override void CreateTrackingLines(Coordinate aCoord, Coordinate bCoord)
        {
            base.CreateTrackingLines(aCoord, bCoord);
        }

        public override void CreateTrackingLines(TrackingLine headLine)
        {
            base.CreateTrackingLines(headLine);
            FillFieldWithTrackingLines(headLine.MainLine, 0.0);
        }

        public override void CreateTrackingLines(TrackingLine trackingLine, DotSpatial.Topology.Angle directionFromLine)
        {
            DotSpatial.Topology.Angle newAngle = new DotSpatial.Topology.Angle(trackingLine.MainLine.Angle + directionFromLine.Radians);
            this.CreateTrackingLines(trackingLine.MainLine.P0, newAngle);
        }

        public override void CreateTrackingLines(TrackingLine trackingLine, DotSpatial.Topology.Angle directionFromLine, double offset)
        {
            DotSpatial.Topology.Angle newAngle = new DotSpatial.Topology.Angle(trackingLine.MainLine.Angle + directionFromLine.Radians);
            FillFieldWithTrackingLines(HelperClassLines.CreateLine(trackingLine.MainLine.P0, newAngle, 5.0), offset);
        }

        public override void UpdateEvents(ILineString positionEquipment, Azimuth direction)
        {
            foreach (TrackingLine trackingLine in _trackingLines)
                if (trackingLine is TrackingLineStartStopEvent)
                    if (trackingLine.Active)
                        if ((trackingLine as TrackingLineStartStopEvent).EventFired(direction, positionEquipment))
                            OnFarmingEvent((trackingLine as TrackingLineStartStopEvent).Message);
        }

        protected void FillFieldWithTrackingLines(ILineSegment baseLine, double offset)
        {
            var negPosOffset = offset;
            var angleOffsetPoint = new DotSpatial.Topology.Angle(baseLine.Angle);
            if (!EquipmentSideOutRight)
            {
                negPosOffset *= -1.0;
                angleOffsetPoint += new DotSpatial.Topology.Angle(HelperClassAngles.DEGREE_90_RAD);
            }
            else
                angleOffsetPoint -= new DotSpatial.Topology.Angle(HelperClassAngles.DEGREE_90_RAD);

            var newCoord = HelperClassCoordinate.ComputePoint(baseLine.P0, angleOffsetPoint.Radians, offset);
            var newLine = HelperClassLines.CreateLine(newCoord, new DotSpatial.Topology.Angle(baseLine.Angle), baseLine.Length);

            _trackingLinesHeadland.Clear();

            double left90 = HelperClassAngles.NormalizeRadian(newLine.Angle + HelperClassAngles.DEGREE_90_RAD);
            double right90 = HelperClassAngles.NormalizeRadian(newLine.Angle - HelperClassAngles.DEGREE_90_RAD);
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
                headlandTrackingLines.AddRange(GetHeadlandLines(distanceFromShell + negPosOffset, leftConstraints, rightConstraints) );
                distanceFromShell += _equipment.WidthOverlap.ToMeters().Value;
            }

            foreach (LineString line in headlandTrackingLines)
                _trackingLinesHeadland.Add(new TrackingLine(line, false));

            IList<ILineString> headLandCoordinates = new List<ILineString>();
            List <Polygon> headLandToCheck = new List<Polygon>();
            IList<ILineString> headLands = GetHeadLandCoordinates(distanceFromShell, leftConstraints, rightConstraints);
            foreach (ILineString headLand in headLands)
            {
                headLandToCheck.Add(new Polygon(headLand.Coordinates));
                foreach(var ring in GetHeadLandCoordinates(0.02, headLand.Coordinates))//add 2cm to make sure we dont get trackingline over headline
                    headLandCoordinates.Add(ring);
            }

            List<LineSegment> headLandLines = new List<LineSegment>();
            foreach (ILineString headLand in headLandCoordinates)
                headLandLines.AddRange(HelperClassLines.CreateLines(headLand.Coordinates));

            IEnvelope fieldEnvelope = _fieldPolygon.Envelope;
            //Get longest projection to make sure we cover the whole field
            ILineSegment baseLineExtended1 = new LineSegment(newLine.Project(fieldEnvelope.TopLeft()), newLine.Project(fieldEnvelope.BottomRight()));
            ILineSegment baseLineExtended2 = new LineSegment(newLine.Project(fieldEnvelope.BottomLeft()), newLine.Project(fieldEnvelope.TopRight()));

            ILineSegment baseLineExtended = baseLineExtended1.Length > baseLineExtended2.Length ? baseLineExtended1 : baseLineExtended2;

            List<ILineSegment> linesExtended = new List<ILineSegment>();
            linesExtended.Add(baseLineExtended);
            ILineSegment extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthOverlap.ToMeters().Value);
            int lineIteriator = 2;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Add(extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthOverlap.ToMeters().Value * lineIteriator);
                lineIteriator++;
            }

            extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthOverlap.ToMeters().Value);
            lineIteriator = 2;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Add(extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthOverlap.ToMeters().Value * lineIteriator);
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
                    var line = new LineSegment(lineCoordinates[0], lineCoordinates[1] );
                    if ((int)line.Angle != (int)newLine.Angle)
                        trackingLines.Add(new LineString(new List<Coordinate>() { line.P1, line.P0 }));
                    else
                        trackingLines.Add(new LineString(new List<Coordinate>() { line.P0, line.P1 }));
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
                                if ((int)line.Angle != (int)newLine.Angle)
                                    trackingLines.Add(new LineString(new List<Coordinate>() { line.P1, line.P0 }));
                                else
                                    trackingLines.Add(new LineString(new List<Coordinate>() { line.P0, line.P1 }));
                                lineCoordinates.RemoveAt(j);
                                lineCoordinates.RemoveAt(0);
                                j = 1;
                            }
                            else
                                j++;
                        }
                    }
                }
                lineCoordinates.Clear();
            }
            AddTrackingLines(trackingLines);
        }

        protected override void AddTrackingLines(IList<LineString> trackingLines)
        {
            _trackingLines.Clear();
                foreach (LineString line in trackingLines)
                    _trackingLines.Add(new TrackingLineStartStopEvent(line, _startDistance, _stopDistance));
        }

        protected void AddTrackingLines(IList<LineString> trackingLines, IList<IGeometry> startPoints, IList<IGeometry> endPoints)
        {
            _trackingLines.Clear();
            if (_startDistance == double.MinValue || _stopDistance == double.MinValue)
                foreach (LineString line in trackingLines)
                    _trackingLines.Add(new TrackingLine(line, false));
            else
                for (int i = 0; i < trackingLines.Count; i++)
                    _trackingLines.Add(new TrackingLineStartStopEvent(trackingLines[i], _startDistance, _stopDistance));

        }

        #region IStateObjectImplementation

        public override object StateObject
        {
            get
            {
                List<SimpleLine> trackingLines = new List<SimpleLine>();
                foreach (TrackingLine trackingLine in _trackingLines)
                    trackingLines.Add(new SimpleLine(new List<Coordinate>(trackingLine.Line.Coordinates)));
                List<SimpleLine> trackingLinesHeadland = new List<SimpleLine>();
                foreach (TrackingLine trackingLineHeadland in _trackingLinesHeadland)
                    trackingLinesHeadland.Add(new SimpleLine(new List<Coordinate>(trackingLineHeadland.Line.Coordinates)));
                return new PlowingModeState(trackingLines, trackingLinesHeadland, _startDistance, _stopDistance);
            }
        }

        public override Type StateType
        {
            get { return typeof(PlowingModeState); }
        }

        public override void RestoreObject(object restoredState)
        {
            PlowingModeState plowingModeState = (PlowingModeState)restoredState;
            _startDistance = plowingModeState.StartDistance;
            _stopDistance = plowingModeState.StopDistance;
            foreach (SimpleLine line in plowingModeState.TrackingLines)
                _trackingLines.Add(new TrackingLineStartStopEvent(new LineString(line.Line), _startDistance, _stopDistance));
            foreach (SimpleLine line in plowingModeState.TrackingLinesHeadLand)
                _trackingLinesHeadland.Add(new TrackingLine(new LineString(line.Line), true));
        }

        #endregion
    }
}
