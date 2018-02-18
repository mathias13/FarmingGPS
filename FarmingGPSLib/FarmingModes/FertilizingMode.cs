using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.FarmingModes.Tools;
using FarmingGPSLib.Equipment;


namespace FarmingGPSLib.FarmingModes
{
    public class FertilizingMode : GeneralHarrowingMode
    {
        private double _startDistance = double.NaN;

        private double _stopDistance = double.NaN;

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

        public override void UpdateEvents(Coordinate position, DotSpatial.Positioning.Azimuth direction)
        {
            base.UpdateEvents(position, direction);
            foreach (TrackingLine trackingLine in _trackingLines)
                if(trackingLine is TrackingLineStartStopEvent)
                    if ((trackingLine as TrackingLineStartStopEvent).EventFired(direction, position))
                        OnFarmingEvent((trackingLine as TrackingLineStartStopEvent).Message);
        }

        protected override void AddTrackingLines(IList<LineString> trackingLines)
        {
            _trackingLines.Clear();
            if(Double.IsNaN(_startDistance) || Double.IsNaN(_stopDistance))
                foreach (LineString line in trackingLines)
                    _trackingLines.Add(new TrackingLine(line));
            else
                foreach (LineString line in trackingLines)
                    _trackingLines.Add(new TrackingLineStartStopEvent(line, _startDistance, _stopDistance));

        }
    }
}
