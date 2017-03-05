using DotSpatial.Positioning;
using System.Collections.Generic;
using System;

namespace FarmingGPSLib.FieldItems
{
    public class FieldCreatedEventArgs : EventArgs
    {
        private Field _field;

        public FieldCreatedEventArgs(Field field)
        {
            _field = field;
        }

        public Field Field
        {
            get { return _field; }
        }
    }
}
