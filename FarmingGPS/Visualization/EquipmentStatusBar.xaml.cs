using FarmingGPSLib.Equipment;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace FarmingGPS.Visualization
{
    /// <summary>
    /// Interaction logic for EquipmentStatus.xaml
    /// </summary>
    public partial class EquipmentStatusBar : UserControl
    {

        public EquipmentStatusBar()
        {
            InitializeComponent();
        }
        public void RegisterEquipmentControl(IEquipmentControl equipmentControl)
        {
            equipmentControl.StatusUpdate += EquipmentControl_StatusUpdate;
        }

        private void EquipmentControl_StatusUpdate(object sender, EventArgs e)
        {
            if (Dispatcher.Thread.Equals(Thread.CurrentThread))
            {
                IEquipmentControl equipmentControl = sender as IEquipmentControl;
                SetValue(Started, equipmentControl.Running);
                SetValue(ConnectedState, equipmentControl.Connected);
            }
            else
                Dispatcher.BeginInvoke(new Action<object, EventArgs>(EquipmentControl_StatusUpdate), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
        }

        #region Dependency Properties

        protected static readonly DependencyProperty ConnectedState = DependencyProperty.RegisterAttached("ConnectedState", typeof(bool), typeof(EquipmentStatusBar));

        protected static readonly DependencyProperty Started = DependencyProperty.RegisterAttached("Started", typeof(bool), typeof(EquipmentStatusBar));

        #endregion
    }
}
