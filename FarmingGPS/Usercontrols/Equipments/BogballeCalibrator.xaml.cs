using FarmingGPSLib.Equipment.BogBalle;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FarmingGPS.Usercontrols.Equipments
{
    /// <summary>
    /// Interaction logic for BogballeL2Plus.xaml
    /// </summary>
    public partial class BogballeCalibrator : UserControl
    {
        Calibrator _calibrator;

        public BogballeCalibrator(Calibrator calibrator)
        {
            InitializeComponent();

            _calibrator = calibrator;
            _calibrator.ValuesUpdated += Calibrator_ValuesUpdated;
            _calibrator.IsConnectedChanged += Calibrator_IsConnectedChanged;
        }

        private void Calibrator_IsConnectedChanged(object sender, bool e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
                SetValue(ConnectedState, e);
            else
                Dispatcher.BeginInvoke(new Action<object, bool>(Calibrator_IsConnectedChanged), DispatcherPriority.Render, sender, e);
        }

        private void Calibrator_ValuesUpdated(object sender, EventArgs e)
        {
            if (Dispatcher.Thread.Equals(System.Threading.Thread.CurrentThread))
            {
                SetValue(Started, _calibrator.Started);
                SetValue(ActualSpreadingRate, _calibrator.ActualSpreadingRate);
                SetValue(SetSpreadingRate, _calibrator.SetSpreadingRate);
                SetValue(PTO, _calibrator.PTO);
                SetValue(Speed, _calibrator.Speed);
                SetValue(Tara, _calibrator.Tara);
            }
            else
                Dispatcher.BeginInvoke(new Action<object, EventArgs>(Calibrator_ValuesUpdated), DispatcherPriority.Render, sender, e);
        }

        #region Dependency Properties

        protected static readonly DependencyProperty ConnectedState = DependencyProperty.Register("ConnectedState", typeof(bool), typeof(BogballeCalibrator));

        protected static readonly DependencyProperty Started = DependencyProperty.Register("Started", typeof(bool), typeof(BogballeCalibrator));

        protected static readonly DependencyProperty ActualSpreadingRate = DependencyProperty.Register("ActualSpreadingRate", typeof(int), typeof(BogballeCalibrator));

        protected static readonly DependencyProperty SetSpreadingRate = DependencyProperty.Register("SetSpreadingRate", typeof(int), typeof(BogballeCalibrator));

        protected static readonly DependencyProperty PTO = DependencyProperty.Register("PTO", typeof(int), typeof(BogballeCalibrator));

        protected static readonly DependencyProperty Speed = DependencyProperty.Register("Speed", typeof(float), typeof(BogballeCalibrator));

        protected static readonly DependencyProperty Tara = DependencyProperty.Register("Tara", typeof(int), typeof(BogballeCalibrator));
        
        #endregion

        private void BTN_START_Click(object sender, RoutedEventArgs e)
        {
            _calibrator.Start();
        }

        private void BTN_STOP_Click(object sender, RoutedEventArgs e)
        {
            _calibrator.Stop();
        }
    }
}
