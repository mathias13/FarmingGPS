using DotSpatial.Positioning;
using FarmingGPSLib.Settings;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FarmingGPS.Usercontrols
{
    /// <summary>
    /// Interaction logic for FarmingMode.xaml
    /// </summary>
    public partial class FarmingMode : UserControl , ISettingsChanged
    {
        public FarmingMode()
        {
            InitializeComponent();
            HeadLandType.SelectionChanged += HeadLandType_SelectionChanged;
        }
        

        public event EventHandler<string> SettingChanged;

        #region Public Properties

        public double Overlap
        {
            get { return NumericOverlap.Value.Value; }
        }

        public int Headlands
        {
            get { return NumericHeadland.Value.Value; }
        }

        public bool HeadLandWidthUsed
        {
            get { return HeadLandType.SelectedIndex == 1; }
        }

        public Distance HeadLandWidth
        {
            get { return Distance.FromMeters(NumericHeadlandWidth.Value.Value); }
        }

        public bool EquipmentSideOutRight
        {
            get { return HeadLandOrientation.SelectedIndex == 0; }
        }

        #endregion

        private void ButtonChoose_Click(object sender, RoutedEventArgs e)
        {
            if (SettingChanged != null)
                SettingChanged.Invoke(this, String.Empty);
        }

        public void RegisterSettingEvent(ISettingsChanged settings)
        {
            throw new NotImplementedException();
        }

        private void HeadLandType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HeadLandType.SelectedIndex == 0)
            {
                NumericHeadlandGrid.Visibility = Visibility.Visible;
                NumericHeadlandWidthGrid.Visibility = Visibility.Collapsed;
            }
            else if (HeadLandType.SelectedIndex == 1)
            {
                NumericHeadlandGrid.Visibility = Visibility.Collapsed;
                NumericHeadlandWidthGrid.Visibility = Visibility.Visible;
            }


        }
    }
}
