using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.Vechile
{
    public struct PositionUpdateStruct
    {           
        private Azimuth _bearing;

        private Position _position;

        public PositionUpdateStruct(Azimuth bearing, Position position)
        {
            _bearing = bearing;
            _position = position;
        }

        public Azimuth Bearing
        {
            get { return _bearing; }
        }

        public Position Position
        {
            get { return _position; }
        }
    }

    public class PositionUpdateEventArgs : EventArgs
    {
        private PositionUpdateStruct _positionUpdate;

        public PositionUpdateEventArgs(PositionUpdateStruct positionUpdate)
        {
            _positionUpdate = positionUpdate;
        }

        public PositionUpdateStruct PositionUpdate
        {
            get { return _positionUpdate; }
        }
    }
}
