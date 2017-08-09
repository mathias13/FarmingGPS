using DotSpatial.Topology;
using System;

namespace FarmingGPSLib.FieldItems
{
    public class PolygonUpdatedEventArgs : EventArgs
    {
        private int _id;

        private IPolygon _polygon;

        private ILineString _newCoordinates;

        private bool _redrawPolygon;

        public PolygonUpdatedEventArgs(int id, IPolygon polygon, ILineString newCoordinates, bool redrawPolygon)
        {
            _id = id;
            _polygon = polygon;
            _newCoordinates = newCoordinates;
            _redrawPolygon = redrawPolygon;
        }

        public int ID
        {
            get { return _id; }
        }

        public IPolygon Polygon
        {
            get { return _polygon; }
        }

        public ILineString NewCoordinates
        {
            get { return _newCoordinates; }
        }

        public bool RedrawPolygon
        {
            get { return _redrawPolygon; }
        }
    }
}
