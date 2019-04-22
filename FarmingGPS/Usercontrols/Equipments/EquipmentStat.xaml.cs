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
using FarmingGPSLib.Equipment;
using System.Windows.Threading;

namespace FarmingGPS.Usercontrols.Equipments
{
    /// <summary>
    /// Interaction logic for EquipmentStat.xaml
    /// </summary>
    public partial class EquipmentStat : UserControl
    {
        IEquipmentStat _equipmentStat;

        public EquipmentStat(IEquipmentStat equipmentStat)
        {
            InitializeComponent();
            _equipmentStat = equipmentStat;
            _equipmentStat.StatUpdated += EquipmentStat_StatUpdated;
        }

        private void EquipmentStat_StatUpdated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(UpdateValues), DispatcherPriority.Render);
        }

        private void UpdateValues()
        {
            SetValue(TotalInput, (int)_equipmentStat.TotalInput);
            SetValue(LeftInEquipment, (int)_equipmentStat.Content);
            SetValue(LeftInEquipmentPercent, (int)_equipmentStat.ContentLeft);
        }

        #region Dependency Properties

        protected static readonly DependencyProperty TotalInput = DependencyProperty.Register("TotalInput", typeof(int), typeof(EquipmentStat));

        protected static readonly DependencyProperty LeftInEquipment = DependencyProperty.Register("LeftInEquipment", typeof(int), typeof(BogballeCalibrator));

        protected static readonly DependencyProperty LeftInEquipmentPercent = DependencyProperty.Register("LeftInEquipmentPercent", typeof(int), typeof(EquipmentStat));

        protected static readonly DependencyProperty AddedContent = DependencyProperty.Register("AddedContent", typeof(int), typeof(EquipmentStat));

        #endregion

        private void BTN_RESET_CONTENT_Click(object sender, RoutedEventArgs e)
        {
            _equipmentStat.ResetTotal();
        }

        private void BTN_ADD_CONTENT_Click(object sender, RoutedEventArgs e)
        {
            _equipmentStat.AddedContent((int)GetValue(AddedContent));
        }
    }
}
