using System;
using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.Equipment;


namespace FarmingGPSLib.FarmingModes
{
    public class SeedingMode : GeneralHarrowingMode
    {
        [Serializable]
        public struct SeedingModeState
        {
            public List<SimpleLine> TrackingLines;

            public List<SimpleLine> StartLines;

            public List<SimpleLine> EndLines;

            public List<SimpleLine> TrackingLinesHeadLand;

            public double StartDistance;

            public double StopDistance;

            public SeedingModeState(List<SimpleLine> trackingLines, List<SimpleLine> startLines, List<SimpleLine> endLines, List<SimpleLine> trackingLinesHeadLand, double startDistance, double stopDistance)
            {
                TrackingLines = trackingLines;
                StartLines = startLines;
                EndLines = endLines;
                TrackingLinesHeadLand = trackingLinesHeadLand;
                StartDistance = startDistance;
                StopDistance = stopDistance;
            }
        }

        private double _startDistance = double.MinValue;

        private double _stopDistance = double.MinValue;

        public SeedingMode() : base()
        { }

        public SeedingMode(IField field)
            : base(field)
        {
        }

        public SeedingMode(IField field, IEquipment equipment, int headLandTurns)
            : base(field, equipment, headLandTurns)
        {
            if(equipment is IEquipmentControl)
            {
                IEquipmentControl equipmentControl = equipment as IEquipmentControl;
                _startDistance = equipmentControl.StartDistance;
                _stopDistance = equipmentControl.StopDistance;
            }
        }

        public override void UpdateEvents(ILineString positionEquipment, DotSpatial.Positioning.Azimuth direction)
        {
            base.UpdateEvents(positionEquipment, direction);
            foreach (TrackingLine trackingLine in _trackingLines)
                if (trackingLine is TrackingLineStartStopEvent)
                    if (trackingLine.Active)
                        if ((trackingLine as TrackingLineStartStopEvent).EventFired(direction, positionEquipment))
                            OnFarmingEvent((trackingLine as TrackingLineStartStopEvent).Message);
        }

        protected override void AddTrackingLines(IList<LineString> trackingLines, IList<IGeometry> startPoints, IList<IGeometry> endPoints)
        {
            if (trackingLines.Count != startPoints.Count || trackingLines.Count != endPoints.Count)
                throw new ArgumentException("Not the same amount of start and endpoints as trackinglines", "startPoint, endpoints");
            _trackingLines.Clear();
            if(_startDistance == double.MinValue || _stopDistance == double.MinValue)
                foreach (LineString line in trackingLines)
                    _trackingLines.Add(new TrackingLine(line, false));
            else
                for(int i = 0; i < trackingLines.Count; i++)
                    _trackingLines.Add(new TrackingLineStartStopEvent(trackingLines[i], startPoints[i], endPoints[i], _startDistance, _stopDistance));

        }

        #region IStateObjectImplementation

        public override object StateObject
        {
            get
            {
                List<SimpleLine> trackingLines = new List<SimpleLine>();
                List<SimpleLine> startLines = new List<SimpleLine>();
                List<SimpleLine> endLines = new List<SimpleLine>();
                foreach (TrackingLine trackingLine in _trackingLines)
                {
                    trackingLines.Add(new SimpleLine(trackingLine.Line.Coordinates));
                    startLines.Add(new SimpleLine(trackingLine.StartPoint.Coordinates));
                    endLines.Add(new SimpleLine(trackingLine.EndPoint.Coordinates));
                }                
                List<SimpleLine> trackingLinesHeadland = new List<SimpleLine>();
                foreach (TrackingLine trackingLineHeadland in _trackingLinesHeadland)
                    trackingLinesHeadland.Add(new SimpleLine(trackingLineHeadland.Line.Coordinates));

                return new SeedingModeState(trackingLines, startLines, endLines, trackingLinesHeadland, _startDistance, _stopDistance);
            }
        }

        public override Type StateType
        {
            get { return typeof(SeedingModeState); }
        }

        public override void RestoreObject(object restoredState)
        {
            SeedingModeState seedingModeState = (SeedingModeState)restoredState;
            _startDistance = seedingModeState.StartDistance;
            _stopDistance = seedingModeState.StopDistance;
            List<LineString> trackingLines = new List<LineString>();
            foreach (SimpleLine line in seedingModeState.TrackingLines)
                trackingLines.Add(new LineString(line.LineCoordinateArray));

            List<IGeometry> startLines = new List<IGeometry>();
            foreach (SimpleLine line in seedingModeState.StartLines)
            {
                if (line.Line.Length == 1)
                    startLines.Add(new Point(line.LineCoordinateArray[0]));
                else
                    startLines.Add(new LineString(line.LineCoordinateArray));
            }

            List<IGeometry> endLines = new List<IGeometry>();
            foreach (SimpleLine line in seedingModeState.EndLines)
            {
                if (line.Line.Length == 1)
                    endLines.Add(new Point(line.LineCoordinateArray[0]));
                else
                    endLines.Add(new LineString(line.LineCoordinateArray));
            }

            AddTrackingLines(trackingLines, startLines, endLines);
            foreach (SimpleLine line in seedingModeState.TrackingLinesHeadLand)
                _trackingLinesHeadland.Add(new TrackingLine(new LineString(line.LineCoordinateArray), true));
        }

        #endregion
    }
}
