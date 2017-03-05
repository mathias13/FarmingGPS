using DotSpatial.Positioning;
using System.Collections.Generic;
using System;

namespace FarmingGPSLib.FieldItems
{
    public class FieldCreatedEventArgs : EventArgs
    {
        private Field _field;

        private FieldTracker _fieldTracker;

        public FieldCreatedEventArgs(Field field, FieldTracker fieldTracker)
        {
            _field = field;
            _fieldTracker = fieldTracker;
        }

        public Field Field
        {
            get { return _field; }
        }

        public FieldTracker FieldTracker
        {
            get { return _fieldTracker; }
        }
    }
}
