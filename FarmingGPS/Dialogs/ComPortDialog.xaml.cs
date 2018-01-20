using System.IO.Ports;
using System.Windows;

namespace FarmingGPS.Dialogs
{
    /// <summary>
    /// Interaction logic for ComPortDialog.xaml
    /// </summary>
    public partial class ComPortDialog : Window
    {
        public ComPortDialog()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
                ComboBoxPort.Items.Add(port.ToString());
            if(ports.Length > 0)
                ComboBoxPort.SelectedIndex = 0;
        }

        public string ComPort
        {
            get
            {
                if (ComboBoxPort.SelectedItem != null)
                    return ComboBoxPort.SelectedItem as string;
                else
                    return string.Empty;
            }
        }

        private void BTN_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
