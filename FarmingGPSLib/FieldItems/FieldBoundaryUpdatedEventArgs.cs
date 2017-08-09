using DotSpatial.Positioning;
using System.Collections.Generic;
using System;

namespace FarmingGPSLib.FieldItems
{
    public class FieldBoundaryUpdatedEventArgs : EventArgs
    {
        private List<Position> _boundary;

        public FieldBoundaryUpdatedEventArgs(List<Position> boundary)
        {
            _boundary = boundary;
        }

        public List<Position> Boundary
        {
            get { return _boundary; }
        }
    }
}
