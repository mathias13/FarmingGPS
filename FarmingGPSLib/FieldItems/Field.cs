﻿using DotSpatial.Positioning;
using DotSpatial.Projections;
using System;
using System.Collections.Generic;

namespace FarmingGPSLib.FieldItems
{
    public class Field : FieldBase
    {
        #region Private Variables

        #endregion

        #region Constructors

        public Field() : base() { }

        public Field(IList<Position> positions, ProjectionInfo proj) : base(positions, proj) { }

        #endregion

        #region Public Methods

        public void AddPoint(Position newPoint, Position precedingPoint)
        {
            lock (_syncObject)
            {
                if (precedingPoint == null)
                    _positions.Insert(0, newPoint);
                else
                {
                    int precedingPointIndex = _positions.IndexOf(precedingPoint);
                    if (precedingPointIndex == -1)
                        throw new ArgumentException("Preceding point doesn't exist");
                    else
                        _positions.Insert(precedingPointIndex + 1, newPoint);
                }
                ReloadPolygon();
            }
        }

        public void DeletePoint(Position pointToRemove)
        {
            lock (_syncObject)
            {
                _positions.Remove(pointToRemove);
                ReloadPolygon();
            }
        }
        
        #endregion

        #region Private Methods

        #endregion

        #region Properties

        #endregion
    }
}
