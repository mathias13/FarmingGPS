using DotSpatial.Positioning;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using FarmingGPSLib.FieldItems;

namespace FarmingGPS.Visualization
{
    /// <summary>
    /// Interaction logic for WorkedAreaBar.xaml
    /// </summary>
    public partial class WorkedAreaBar : UserControl
    {
        public enum AreaUnitExt
        {
            Acres,
            SquareCentimeters,
            SquareFeet,
            SquareInches,
            SquareKilometers,
            SquareMeters,
            SquareNauticalMiles,
            SquareStatuteMiles,
            Hectars
        }

        private AreaUnitExt _unit = AreaUnitExt.Acres;

        private Area _fieldArea = Area.Minimum;

        public WorkedAreaBar()
        {
            InitializeComponent();
        }

        #region Public Properties

        public AreaUnitExt Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }
        #endregion

        #region Public Methods

        public void SetWorkedArea(Area area)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                switch (_unit)
                {
                    case AreaUnitExt.Acres:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} acre", area.ToAcres().Value.ToString("0.00"), _fieldArea.ToAcres().ToString("0.00")));
                        break;

                    case AreaUnitExt.SquareCentimeters:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} cm²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnitExt.SquareFeet:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} ft²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnitExt.SquareInches:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} in²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnitExt.SquareKilometers:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} km²", area.ToAcres().Value.ToString("0.00"), _fieldArea.ToAcres().ToString("0.00")));
                        break;

                    case AreaUnitExt.SquareMeters:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} m²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnitExt.SquareNauticalMiles:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} nm²", area.ToAcres().Value.ToString("0.0"), _fieldArea.ToAcres().ToString("0.0")));
                        break;
                        
                    case AreaUnitExt.SquareStatuteMiles:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} sm²", area.ToAcres().Value.ToString("0.0"), _fieldArea.ToAcres().ToString("0.0")));
                        break;

                    case AreaUnitExt.Hectars:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} ha", (area.ToSquareMeters().Value / 10000).ToString("0.0"), (_fieldArea.ToSquareMeters().Value / 10000).ToString("0.0")));
                        break;
                }

                double worked = area.ToMetricUnitType().Value;
                double field = _fieldArea.ToMetricUnitType().Value;

                SetValue(WidthPercentage, _backGround.ActualWidth * Math.Min(1.0, worked / field));
                double width = (double)GetValue(WidthPercentage);
            }
            else
                Dispatcher.BeginInvoke(new Action<Area>(SetWorkedArea), System.Windows.Threading.DispatcherPriority.Normal, area);
        }

        public void SetField(IField field)
        {
            _fieldArea = field.FieldArea;
        }

        #endregion

        #region Dependency Properties

        protected static readonly DependencyProperty WorkedAreaString = DependencyProperty.RegisterAttached("WorkedAreaString", typeof(string), typeof(WorkedAreaBar));

        protected static readonly DependencyProperty WidthPercentage = DependencyProperty.RegisterAttached("WidthPercentage", typeof(double), typeof(WorkedAreaBar));

        #endregion
    }
}
