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
using System.Windows.Shapes;
using GMap.NET.WindowsPresentation;

namespace FarmingGPS.Usercontrols.GoogleMaps
{
    /// <summary>
    /// Interaction logic for MarkerBlueDot.xaml
    /// </summary>
    public partial class MarkerBlueDot : UserControl
    {
        private GMapMarker _marker;

        public MarkerBlueDot(GMapMarker marker)
        {
            InitializeComponent();
            _marker = marker;
            this.Loaded += new RoutedEventHandler(MarkerBlueDot_Loaded);
            this.SizeChanged += new SizeChangedEventHandler(MarkerBlueDot_SizeChanged);
        }

        private void MarkerBlueDot_Loaded(object sender, RoutedEventArgs e)
        {
            if (icon.Source.CanFreeze)
            {
                icon.Source.Freeze();
            }
        }

        private void MarkerBlueDot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _marker.Offset = new Point(-e.NewSize.Width / 2, -e.NewSize.Height);
        }
    }
}
