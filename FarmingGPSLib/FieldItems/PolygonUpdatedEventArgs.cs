using System;
using System.Collections.Generic;
using DotSpatial.Topology;

namespace FarmingGPSLib.FieldItems
{
    public class PolygonUpdatedEventArgs : EventArgs
    {
        private int _id;

        private IPolygon _polygon;

        private ILineString _newCoordinates;

        private bool _polygonHolesChanged;

        public PolygonUpdatedEventArgs(int id, IPolygon polygon, ILineString newCoordinates, bool polygonHolesChanged)
        {
            _id = id;
            _polygon = polygon;
            _newCoordinates = newCoordinates;
            _polygonHolesChanged = polygonHolesChanged;
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

        public bool PolygonHolesChanged
        {
            get { return _polygonHolesChanged; }
        }
    }
}
