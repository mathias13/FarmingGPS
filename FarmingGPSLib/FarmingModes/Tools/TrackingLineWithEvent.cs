using System;
using System.Collections.Generic;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;

namespace FarmingGPSLib.FarmingModes.Tools
{                                                                    
    public class TrackingLineWithEvent : TrackingLine, IFarmingEvent
    {
        #region Private Variables

        protected string _eventMessage;

        protected double _distanceToEvent;

        #endregion

        public TrackingLineWithEvent(ILineString line, string eventMessage, double distanceToEvent, ILineSegment end1, ILineSegment end2): base(line)
        {
            _eventMessage = eventMessage;
            _distanceToEvent = distanceToEvent;       

        }

        public string Message
        {
            get { return _eventMessage; }
        }

        public bool EventFired(Angle angle, Coordinate coord)
        {
            return false;
        }
    }
}
