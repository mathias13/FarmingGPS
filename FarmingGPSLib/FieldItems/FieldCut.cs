using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.FieldItems
{
    public struct FieldCut
    {
        Position _startCut;

        Position _endCut;

        public FieldCut(Position startCut, Position endCut)
        {
            _startCut = startCut;
            _endCut = endCut;
        }

        public Position StartCut
        {
            get { return _startCut; }
        }

        public Position EndCut
        {
            get { return _endCut; }
        }
    }
}
