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
using System.Windows.Shapes;

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
