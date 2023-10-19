using FarmingGPSLib.Equipment;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FarmingGPS.Visualization
{
    /// <summary>
    /// Interaction logic for WorkedAreaBar.xaml
    /// </summary>
    public partial class EquipmentLevelBar : UserControl
    {
        public EquipmentLevelBar()
        {
            InitializeComponent();

        }
        
        #region Public Methods

        public void RegisterEquipmentStat(IEquipmentStat equipmentStat)
        {
            equipmentStat.StatUpdated += EquipmentStat_StatUpdated;
        }

        #endregion

        #region Private Methods

        private void EquipmentStat_StatUpdated(object sender, EventArgs e)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                IEquipmentStat equipmentStat = sender as IEquipmentStat;
                double fillLevel = Math.Min(equipmentStat.ContentLeft / 100.0, 1.0);
                fillLevel = Math.Max(fillLevel, 0.0);

                SetValue(FillLevelString, fillLevel.ToString("0 %"));
                SetValue(WidthPercentage, _backGround.ActualWidth * fillLevel);
                SetValue(FillColor, fillLevel > 10.0 ? Colors.LightGreen : Colors.Red);
            }
            else
                Dispatcher.BeginInvoke(new Action<object, EventArgs>(EquipmentStat_StatUpdated), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
        }

        #endregion

        #region Dependency Properties

        protected static readonly DependencyProperty FillLevelString = DependencyProperty.RegisterAttached("FillLevelString", typeof(string), typeof(EquipmentLevelBar));

        protected static readonly DependencyProperty WidthPercentage = DependencyProperty.RegisterAttached("WidthPercentage", typeof(double), typeof(EquipmentLevelBar));

        protected static readonly DependencyProperty FillColor = DependencyProperty.RegisterAttached("FillColor", typeof(Brush), typeof(EquipmentLevelBar));

        #endregion
    }
}
