using System;

namespace FarmingGPSLib.FieldItems
{
    public class PolygonDeletedEventArgs : EventArgs
    {
        private int _id;

        public PolygonDeletedEventArgs(int id)
        {
            _id = id;
        }

        public int ID
        {
            get { return _id; }
        }
    }
}
