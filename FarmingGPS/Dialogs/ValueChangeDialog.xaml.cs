using System.Windows;

namespace FarmingGPS.Dialogs
{
    /// <summary>
    /// Interaction logic for OKDialog.xaml
    /// </summary>
    public partial class ValueChangeDialog : Window
    {
        public ValueChangeDialog()
        {
            InitializeComponent();
        }

        public ValueChangeDialog(float value, float min, float max, float increment, string format) : this()
        {
            VALUE.Value = value;
            VALUE.Minimum = min;
            VALUE.Maximum = max;
            VALUE.Increment = increment;
            VALUE.FormatString = format;
        }

        public float Value
        {
            get { return VALUE.Value.Value; }
        }

        private void BTN_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BTN_CANCEL_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
