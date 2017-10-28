using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Threading;
using System.Windows.Shapes;
using DotSpatial.Positioning;

namespace FarmingGPS.Visualization
{
    /// <summary>
    /// Interaction logic for SpeedBar.xaml
    /// </summary>
    public partial class SpeedBar : UserControl
    {
        private SpeedUnit _unit = SpeedUnit.KilometersPerHour;
        
        public SpeedBar()
        {
            InitializeComponent();
        }

        #region Public Properties
            
        public SpeedUnit Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }
            
        #endregion

        #region Public Methods

        public void SetSpeed(Speed speed)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                switch (_unit)
                {
                    case SpeedUnit.FeetPerSecond:
                        SetValue(SpeedString, String.Format("{0} ft/s", speed.ToFeetPerSecond().Value.ToString("0.0")));
                        break;

                    case SpeedUnit.KilometersPerHour:
                        SetValue(SpeedString, String.Format("{0} km/h", speed.ToKilometersPerHour().Value.ToString("0.0")));
                        break;

                    case SpeedUnit.KilometersPerSecond:
                        SetValue(SpeedString, String.Format("{0} km/s", speed.ToKilometersPerSecond().Value.ToString("0.0")));
                        break;

                    case SpeedUnit.Knots:
                        SetValue(SpeedString, String.Format("{0} knots", speed.ToKnots().Value.ToString("0.0")));
                        break;

                    case SpeedUnit.MetersPerSecond:
                        SetValue(SpeedString, String.Format("{0} m/s", speed.ToMetersPerSecond().Value.ToString("0.0")));
                        break;

                    case SpeedUnit.StatuteMilesPerHour:
                        SetValue(SpeedString, String.Format("{0} mph", speed.ToStatuteMilesPerHour().Value.ToString("0.0")));
                        break;
                }
            }
            else
                Dispatcher.BeginInvoke(new Action<Speed>(SetSpeed), System.Windows.Threading.DispatcherPriority.Render, speed);
        }
        
        #endregion

        #region Dependency Properties

        protected static readonly DependencyProperty SpeedString = DependencyProperty.RegisterAttached("SpeedString", typeof(string), typeof(SpeedBar));

        #endregion
    }
}
