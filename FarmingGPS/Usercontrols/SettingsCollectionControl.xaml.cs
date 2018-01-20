using FarmingGPS.Settings;
using System;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit;

namespace FarmingGPS.Usercontrols
{
    /// <summary>
    /// Interaction logic for SettingsCollectionControl.xaml
    /// </summary>
    public partial class SettingsCollectionControl : UserControl, ISettingsChanged
    {
        protected ISettingsCollection _settingCollection;

        #region DependencyProperties

        protected static readonly DependencyProperty HeaderName = DependencyProperty.Register("HeaderName", typeof(string), typeof(SettingsCollectionControl));

        #endregion

        #region ISettingsChanged

        public event EventHandler<string> SettingChanged;

        #endregion

        public ISettingsCollection Settings
        {
            get { return _settingCollection; }
        }

        public SettingsCollectionControl(ISettingsCollection settingCollection)
        {
            InitializeComponent();
            _settingCollection = settingCollection;
            SetValue(HeaderName, _settingCollection.Name);
            foreach (ISetting setting in _settingCollection)
            {
                TextBlock settingHeader = new TextBlock();
                settingHeader.Text = setting.Name;
                settingHeader.Style = (Style)this.FindResource("SETTING_HEADER");
                SettingPanel.Children.Add(settingHeader);
                FrameworkElement settingControl = null;
                if (setting.ValueType == typeof(double) || setting.ValueType == typeof(float))
                {
                    DoubleUpDown doubleUpDown = new DoubleUpDown();
                    doubleUpDown.Value = (double)setting.Value;
                    settingControl = doubleUpDown;
                    settingControl.Style = (Style)FindResource("DOUBLEUPDOWN");
                }
                else if (setting.ValueType == typeof(int))
                {
                    IntegerUpDown integerUpDown = new IntegerUpDown();
                    integerUpDown.Value = (int)setting.Value;
                    settingControl = integerUpDown;
                    settingControl.Style = (Style)FindResource("INTEGERUPDOWN");
                }
                else if (setting.ValueType == typeof(long))
                {
                    LongUpDown longUpDown = new LongUpDown();
                    longUpDown.Value = (long)setting.Value;
                    settingControl = longUpDown;
                    settingControl.Style = (Style)FindResource("LONGUPDOWN");
                }
                else if(setting.ValueType == typeof(string))
                {
                    TextBox textBox = new TextBox();
                    textBox.Text = (string)setting.Value;
                    settingControl = textBox;
                    settingControl.Style = (Style)FindResource("TEXTBOX");
                }
                else if(setting.ValueType == typeof(bool))
                {
                    ComboBox comboBox = new ComboBox();
                    comboBox.Items.Add("Falskt");
                    comboBox.Items.Add("Sant");
                    comboBox.SelectedIndex = (bool)setting.Value ? 1 : 0;
                    settingControl = comboBox;
                    settingControl.Style = (Style)FindResource("COMBOBOX");
                }
                else if(setting.ValueType.IsEnum)
                {
                    ComboBox comboBox = new ComboBox();
                    foreach (object item in Enum.GetValues(setting.ValueType))
                        comboBox.Items.Add(item);
                    comboBox.SelectedItem = setting.Value;
                    settingControl = comboBox;
                    settingControl.Style = (Style)FindResource("COMBOBOX");
                }
                else
                {
                    TextBox textBox = new TextBox();
                    textBox.Text = "Inställning stöds ej";
                    textBox.IsEnabled = false;
                    settingControl = textBox;
                    settingControl.Style = (Style)FindResource("TEXTBOX");
                }
                        SettingPanel.Children.Add(settingControl);
            }
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < SettingPanel.Children.Count; i += 2)
            {
                if (SettingPanel.Children[i] is TextBlock)
                {
                    ISetting setting = _settingCollection[(SettingPanel.Children[i] as TextBlock).Text];
                    if (setting.ValueType == typeof(double) || setting.ValueType == typeof(float))
                        setting.Value = Convert.ChangeType((SettingPanel.Children[i + 1] as DoubleUpDown).Value, setting.ValueType);
                    else if (setting.ValueType == typeof(int))
                        setting.Value = Convert.ChangeType((SettingPanel.Children[i + 1] as IntegerUpDown).Value, setting.ValueType);
                    else if (setting.ValueType == typeof(long))
                        setting.Value = Convert.ChangeType((SettingPanel.Children[i + 1] as LongUpDown).Value, setting.ValueType);
                    else if (setting.ValueType == typeof(bool))
                        setting.Value = (SettingPanel.Children[i + 1] as ComboBox).SelectedIndex == 1 ? true : false;
                    else if (setting.ValueType.IsEnum)
                        setting.Value = (SettingPanel.Children[i + 1] as ComboBox).SelectedItem;
                    else if (setting.ValueType == typeof(string))
                        setting.Value = (SettingPanel.Children[i + 1] as TextBox).Text;
                }
            }
            
            if (SettingChanged != null)
                SettingChanged.Invoke(this, String.Empty);
        }
    }
}
