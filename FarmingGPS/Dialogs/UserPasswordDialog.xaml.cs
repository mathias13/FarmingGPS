using System.Windows;

namespace FarmingGPS.Dialogs
{
    /// <summary>
    /// Interaction logic for UserPasswordDialog.xaml
    /// </summary>
    public partial class UserPasswordDialog : Window
    {
        public UserPasswordDialog()
        {
            InitializeComponent();
        }

        public UserPasswordDialog(string username) : this()
        {
            TextBoxUserName.Text = username;
        }

        public string UserName
        {
            get { return TextBoxUserName.Text; }
        }

        public string Password
        {
            get { return TextBoxPassword.Password; }
        }

        private void BTN_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
