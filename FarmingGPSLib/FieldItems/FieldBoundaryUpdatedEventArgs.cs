using GeoAPI.Geometries;
using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FieldItems
{
    public class FieldBoundaryUpdatedEventArgs : EventArgs
    {
        private List<Coordinate> _boundary;

        public FieldBoundaryUpdatedEventArgs(List<Coordinate> boundary)
        {
            _boundary = boundary;
        }

        public List<Coordinate> Boundary
        {
            get { return _boundary; }
        }
    }
}
