using FarmingGPSLib.Equipment.Vaderstad;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FarmingGPS.Dialogs;

namespace FarmingGPS.Usercontrols.Equipments
{
    /// <summary>
    /// Interaction logic for BogballeL2Plus.xaml
    /// </summary>
    public partial class VaderstadController : UserControl
    {
        Controller _controller;

        public VaderstadController(Controller controller)
        {
            InitializeComponent();

            _controller = controller;
            _controller.ValuesUpdated += Controller_ValuesUpdated;
            _controller.IsConnectedChanged += Controller_IsConnectedChanged;
        }

        private void Controller_IsConnectedChanged(object sender, bool e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
                SetValue(ConnectedState, e);
            else
                Dispatcher.BeginInvoke(new Action<object, bool>(Controller_IsConnectedChanged), DispatcherPriority.Render, sender, e);
        }

        private void Controller_ValuesUpdated(object sender, EventArgs e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                SetValue(Started, _controller.Started);
                SetValue(Alarm, _controller.Alarm);
                SetValue(ActualSeedingRate, _controller.ActualSeedingRate);
                SetValue(MaxRateOfTavel, _controller.MaxRateOfTravel);
                SetValue(SetSeedingRate, _controller.SetSeedingRate);
                SetValue(Area, _controller.HA);
                if(_controller.HA != 0.0)
                    SetValue(AvgRate, _controller.SeedUsed / _controller.HA);
                else
                    SetValue(AvgRate, 0.0f);
                SetValue(SeedMotorSpeed, _controller.SeedMotorSpeed);
                SetValue(SeedUsed, _controller.SeedUsed);
                SetValue(CalWeight, _controller.CalibrationWeight);
            }
            else
                Dispatcher.BeginInvoke(new Action<object, EventArgs>(Controller_ValuesUpdated), DispatcherPriority.Render, sender, e);
        }

        #region Dependency Properties

        protected static readonly DependencyProperty ConnectedState = DependencyProperty.Register("ConnectedState", typeof(bool), typeof(VaderstadController));

        protected static readonly DependencyProperty Started = DependencyProperty.Register("Started", typeof(bool), typeof(VaderstadController));

        protected static readonly DependencyProperty Alarm = DependencyProperty.Register("Alarm", typeof(bool), typeof(VaderstadController));

        protected static readonly DependencyProperty ActualSeedingRate = DependencyProperty.Register("ActualSpreadingRate", typeof(int), typeof(VaderstadController));

        protected static readonly DependencyProperty MaxRateOfTavel = DependencyProperty.Register("MaxRateOfTravel", typeof(float), typeof(VaderstadController));

        protected static readonly DependencyProperty SetSeedingRate = DependencyProperty.Register("SetSpreadingRate", typeof(int), typeof(VaderstadController));

        protected static readonly DependencyProperty Area = DependencyProperty.Register("Area", typeof(float), typeof(VaderstadController));

        protected static readonly DependencyProperty AvgRate = DependencyProperty.Register("AvgRate", typeof(float), typeof(VaderstadController));

        protected static readonly DependencyProperty SeedMotorSpeed = DependencyProperty.Register("SeedMotorSpeed", typeof(float), typeof(VaderstadController));

        protected static readonly DependencyProperty SeedUsed = DependencyProperty.Register("SeedUsed", typeof(float), typeof(VaderstadController));

        protected static readonly DependencyProperty CalWeight = DependencyProperty.Register("CalWeight", typeof(float), typeof(VaderstadController));

        #endregion

        private void BTN_START_Click(object sender, RoutedEventArgs e)
        {
            _controller.Start();
        }

        private void BTN_STOP_Click(object sender, RoutedEventArgs e)
        {
            _controller.Stop();
        }

        private void BTN_RATETEST_Click(object sender, RoutedEventArgs e)
        {
            _controller.StartRateTest();
        }

        private void CalWeight_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ValueChangeDialog valueChangeDialog = new ValueChangeDialog(_controller.CalibrationWeight, 0.0f, 5.0f, 0.1f, "0.00 kg");
            if (valueChangeDialog.ShowDialog().Value)
                _controller.ChangeCalibrationWeight(valueChangeDialog.Value);
        }

        private void Rate_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ValueChangeDialog valueChangeDialog = new ValueChangeDialog(_controller.SetSeedingRate, 0.0f, 500.0f, 5.0f, "0 kg/ha");
            if (valueChangeDialog.ShowDialog().Value)
                _controller.ChangeSeedingRate((int)valueChangeDialog.Value);
        }

        private void BTN_RESET_Click(object sender, RoutedEventArgs e)
        {
            _controller.ResetSums();
        }
    }
}
