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
    /// Interaction logic for MarkerRedDot.xaml
    /// </summary>
    public partial class MarkerRedDot : UserControl
    {
        private GMapMarker _marker;

        public MarkerRedDot(GMapMarker marker)
        {
            InitializeComponent();
            _marker = marker;
            this.Loaded += new RoutedEventHandler(MarkerRedDot_Loaded);
            this.SizeChanged += new SizeChangedEventHandler(MarkerRedDot_SizeChanged);
        }

        private void MarkerRedDot_Loaded(object sender, RoutedEventArgs e)
        {
            if (icon.Source.CanFreeze)
            {
                icon.Source.Freeze();
            }
        }

        private void MarkerRedDot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _marker.Offset = new Point(-e.NewSize.Width / 2, -e.NewSize.Height);
        }
    }
}
