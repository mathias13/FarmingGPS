using System.Windows;

namespace FarmingGPS.Dialogs
{
    /// <summary>
    /// Interaction logic for OKDialog.xaml
    /// </summary>
    public partial class OKDialog : Window
    {
        public OKDialog()
        {
            InitializeComponent();
        }

        public OKDialog(string message) : this()
        {
            TEXT_MESSAGE.Text = message;
        }

        private void BTN_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
