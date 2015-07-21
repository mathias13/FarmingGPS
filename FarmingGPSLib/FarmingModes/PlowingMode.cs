using System;
using System.Collections.Generic;
using FarmingGPSLib.FieldItems;
using FarmingGPSLib.Equipment;
using DotSpatial.Positioning;
using DotSpatial.Topology;


namespace FarmingGPSLib.FarmingModes
{
    public class PlowingMode : FarmingModeBase
    {

        public PlowingMode(IField field)
            : base(field)
        {
        }

        public PlowingMode(IField field, IEquipment equipment, Distance headlandSpace, List<int> headLandPositions)
            : base(field)
        {
            List<Coordinate> noHeadlandPolygon = new List<Coordinate>();
            headLandPositions.Sort();
            for (int i = 0; i < headLandPositions.Count; i++)
                ;
        }
    }
}
