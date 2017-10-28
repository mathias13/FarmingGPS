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
        private AreaUnit _unit = AreaUnit.Acres;

        private Area _fieldArea = Area.Minimum;

        public WorkedAreaBar()
        {
            InitializeComponent();
        }

        #region Public Properties

        public AreaUnit Unit
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
                    case AreaUnit.Acres:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} acre", area.ToAcres().Value.ToString("0.00"), _fieldArea.ToAcres().ToString("0.00")));
                        break;

                    case AreaUnit.SquareCentimeters:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} cm²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnit.SquareFeet:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} ft²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnit.SquareInches:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} in²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnit.SquareKilometers:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} km²", area.ToAcres().Value.ToString("0.00"), _fieldArea.ToAcres().ToString("0.00")));
                        break;

                    case AreaUnit.SquareMeters:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} m²", area.ToAcres().Value.ToString("0."), _fieldArea.ToAcres().ToString("0.")));
                        break;

                    case AreaUnit.SquareNauticalMiles:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} nm²", area.ToAcres().Value.ToString("0.0"), _fieldArea.ToAcres().ToString("0.0")));
                        break;
                        
                    case AreaUnit.SquareStatuteMiles:
                        SetValue(WorkedAreaString, String.Format("{0}/{1} sm²", area.ToAcres().Value.ToString("0.0"), _fieldArea.ToAcres().ToString("0.0")));
                        break;
                }

                double worked = area.ToMetricUnitType().Value;
                double field = _fieldArea.ToMetricUnitType().Value;

                SetValue(WidthPercentage, _backGround.ActualWidth * Math.Min(1.0, worked / field));
                double width = (double)GetValue(WidthPercentage);
            }
            else
                Dispatcher.BeginInvoke(new Action<Area>(SetWorkedArea), System.Windows.Threading.DispatcherPriority.Render, area);
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
