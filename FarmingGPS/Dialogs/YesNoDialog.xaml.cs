using System.Windows;

namespace FarmingGPS.Dialogs
{
    /// <summary>
    /// Interaction logic for UserPasswordDialog.xaml
    /// </summary>
    public partial class YesNoDialog : Window
    {
        public YesNoDialog()
        {
            InitializeComponent();
        }

        public YesNoDialog(string message) : this()
        {
            TEXT_MESSAGE.Text = message;
        }
        
        private void BTN_YES_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BTN_NO_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
