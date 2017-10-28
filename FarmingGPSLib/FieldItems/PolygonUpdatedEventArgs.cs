using DotSpatial.Topology;
using System;

namespace FarmingGPSLib.FieldItems
{
    public class PolygonUpdatedEventArgs : EventArgs
    {
        private int _id;

        private IPolygon _polygon;

        public PolygonUpdatedEventArgs(int id, IPolygon polygon)
        {
            _id = id;
            _polygon = polygon;
        }

        public int ID
        {
            get { return _id; }
        }

        public IPolygon Polygon
        {
            get { return _polygon; }
        }
    }
}
