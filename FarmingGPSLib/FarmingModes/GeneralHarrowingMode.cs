using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.GeometriesGraph;
using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using GpsUtilities.HelperClasses;
using System.Collections.Generic;

namespace FarmingGPSLib.FarmingModes
{
    public class GeneralHarrowingMode : FarmingModeBase
    {
        protected IEquipment _equipment;

        protected int _headlandTurns;

        public GeneralHarrowingMode() : base()
        { }

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
            double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            List<LineString> trackingLines = new List<LineString>();
            for (int i = 0; i < _headlandTurns; i++)
            {
                trackingLines.AddRange(GetHeadlandLines(distanceFromShell));
                distanceFromShell += _equipment.WidthExclOverlap.ToMeters().Value;
            }
            foreach (LineString line in trackingLines)
                _trackingLinesHeadland.Add(new TrackingLine(line));
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

        public override void UpdateEvents(Coordinate position, DotSpatial.Positioning.Azimuth direction)
        {
        }

        protected void FillFieldWithTrackingLines(ILineSegment baseLine)
        {
            double distanceFromShell = _equipment.CenterToTip.ToMeters().Value;
            for (int i = 1; i < _headlandTurns; i++)
                distanceFromShell += _equipment.WidthExclOverlap.ToMeters().Value;
            distanceFromShell += _equipment.CenterToTip.ToMeters().Value;
            IList<ILineString> headLandCoordinates = GetHeadLandCoordinates(distanceFromShell + 0.02); //add 2cm to make sure we dont get trackingline over headline
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
            linesExtended.Add(baseLineExtended);
            ILineSegment extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthExclOverlap.ToMeters().Value);
            int lineIteriator = 2;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Add(extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Left, _equipment.WidthExclOverlap.ToMeters().Value * lineIteriator);
                lineIteriator++;
            }

            extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthExclOverlap.ToMeters().Value);
            lineIteriator = 2;
            while (fieldEnvelope.Intersects(extendedLine))
            {
                linesExtended.Add(extendedLine);
                extendedLine = HelperClassLines.ComputeOffsetSegment(baseLineExtended, PositionType.Right, _equipment.WidthExclOverlap.ToMeters().Value * lineIteriator);
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
                    trackingLines.Add(new LineString(new List<Coordinate>() { lineCoordinates[0], lineCoordinates[1] }));
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
                                trackingLines.Add(new LineString(new List<Coordinate>() { lineCoordinates[0], lineCoordinates[j] }));
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
    
        protected virtual void AddTrackingLines(IList<LineString> trackingLines)
        {
            _trackingLines.Clear();
            foreach (LineString line in trackingLines)
                _trackingLines.Add(new TrackingLine(line));
        }
    }
}
