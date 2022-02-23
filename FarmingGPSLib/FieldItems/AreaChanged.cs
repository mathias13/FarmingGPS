using System;
using DotSpatial.Positioning;

namespace FarmingGPSLib.FieldItems
{
    public class AreaChanged : EventArgs
    {
        private Area _area;
        
        public AreaChanged(Area area)
        {
            _area = area;
        }

        public Area Area
        {
            get { return _area; }
        }
    }
}
