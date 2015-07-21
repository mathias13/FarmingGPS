using System;
using System.Collections.Generic;
using DotSpatial.Positioning;
using DotSpatial.Projections;

namespace FarmingGPSLib.FieldItems
{
    public class SubField: FieldBase
    {
        public SubField(ProjectionInfo proj, Field fieldTocut, IList<FieldCut> fieldCuts)
        {
            IList<Position> positions = fieldTocut.BoundaryPositions;
            foreach (FieldCut fieldCut in fieldCuts)
            {
                int startCutIndex = positions.IndexOf(fieldCut.StartCut);
                int endCutIndex = positions.IndexOf(fieldCut.EndCut);
                if (startCutIndex == -1 || endCutIndex == -1)
                    throw new InvalidOperationException("Field cuts is missing in field to cut");

                for (int i = startCutIndex; i < endCutIndex; i++)
                    positions.RemoveAt(startCutIndex + 1);
            }

            _positions = positions;
            _proj = proj;

            ReloadPolygon();
        }
    }
}
