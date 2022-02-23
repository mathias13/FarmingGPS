using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using DotSpatial.Positioning;

namespace FarmingGPSLib.FarmingModes.Tools
{
    public class TrackingLineWithEvent : TrackingLine, IFarmingEvent
    {
        #region Private Variables

        protected string _eventMessage;

        protected double _distanceToEvent;

        #endregion

        public TrackingLineWithEvent(ILineString line, string eventMessage, double distanceToEvent, LineSegment end1, LineSegment end2): base(line, false)
        {
            _eventMessage = eventMessage;
            _distanceToEvent = distanceToEvent;       

        }

        public string Message
        {
            get { return _eventMessage; }
        }

        public bool EventFired(Azimuth directionOfTravel, Coordinate coord)
        {
            return false;
        }

        public bool EventFired(Azimuth directionOfTravel, ILineString equipmentCoords)
        {
            return false;
        }
    }
}
