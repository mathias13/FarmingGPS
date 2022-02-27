using FarmingGPSLib.Equipment;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.FieldItems;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;


namespace FarmingGPSLib.FarmingModes
{
    public class FertilizingMode : GeneralHarrowingMode
    {
        [Serializable]
        public struct FertilizingModeState
        {
            public List<SimpleLine> TrackingLines;

            public List<SimpleLine> TrackingLinesHeadLand;

            public double StartDistance;

            public double StopDistance;

            public FertilizingModeState(List<SimpleLine> trackingLines, List<SimpleLine> trackingLinesHeadLand, double startDistance, double stopDistance)
            {
                TrackingLines = trackingLines;
                TrackingLinesHeadLand = trackingLinesHeadLand;
                StartDistance = startDistance;
                StopDistance = stopDistance;
            }
        }

        private double _startDistance = double.MinValue;

        private double _stopDistance = double.MinValue;

        public FertilizingMode() : base()
        { }

        public FertilizingMode(IField field)
            : base(field)
        {
        }

        public FertilizingMode(IField field, IEquipment equipment, int headLandTurns)
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
                    trackingLines.Add(new SimpleLine(trackingLine.Line.Coordinates));
                List<SimpleLine> trackingLinesHeadland = new List<SimpleLine>();
                foreach (TrackingLine trackingLineHeadland in _trackingLinesHeadland)
                    trackingLinesHeadland.Add(new SimpleLine(trackingLineHeadland.Line.Coordinates));
                return new FertilizingModeState(trackingLines, trackingLinesHeadland, _startDistance, _stopDistance);
            }
        }

        public override Type StateType
        {
            get { return typeof(FertilizingModeState); }
        }

        public override void RestoreObject(object restoredState)
        {
            FertilizingModeState fertilizingModeState = (FertilizingModeState)restoredState;
            _startDistance = fertilizingModeState.StartDistance;
            _stopDistance = fertilizingModeState.StopDistance;
            List<LineString> trackingLines = new List<LineString>();
            foreach (SimpleLine line in fertilizingModeState.TrackingLines)
                trackingLines.Add(new LineString(line.LineCoordinateArray));
            AddTrackingLines(trackingLines, new List<IGeometry>(), new List<IGeometry>());
            foreach (SimpleLine line in fertilizingModeState.TrackingLinesHeadLand)
                _trackingLinesHeadland.Add(new TrackingLine(new LineString(line.LineCoordinateArray), true));
        }

        #endregion
    }
}
