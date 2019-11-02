using DotSpatial.Positioning;
using DotSpatial.Topology;
using System.Collections.Generic;
using System;

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
