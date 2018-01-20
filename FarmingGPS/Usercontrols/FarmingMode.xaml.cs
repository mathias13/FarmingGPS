using FarmingGPS.Settings;
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

        #endregion

        #region DependencyProperties

        protected static readonly DependencyProperty EquipmentOverlap = DependencyProperty.Register("EquipmentOverlap", typeof(double), typeof(FarmingMode));

        protected static readonly DependencyProperty EquipmentWidth = DependencyProperty.Register("EquipmentWidth", typeof(double), typeof(FarmingMode));
        #endregion

        private void ButtonChoose_Click(object sender, RoutedEventArgs e)
        {
            if (SettingChanged != null)
                SettingChanged.Invoke(this, String.Empty);
        }
    }
}
