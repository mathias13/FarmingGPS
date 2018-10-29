using DotSpatial.Data;
using FarmingGPS.Database;
using FarmingGPSLib.Settings;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.Compression;

namespace FarmingGPS.Usercontrols
{
    /// <summary>
    /// Interaction logic for GetEquipmentRate.xaml
    /// </summary>
    public partial class GetEquipmentRate : UserControl, IDatabaseSettings, ISettingsChanged
    {
        public const string EQUIPMENTRATE_CHOOSEN = "EQUIPMENTRATE_CHOOSEN";

        private DatabaseHandler _database;

        private EquipmentRateFile[] _equipmentRates;

        private Shapefile _equipmentRateChoosen;

        private int? _fieldChoosen;

        private int? _equipmentChoosen;
        
        protected static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(BitmapImage), typeof(GetEquipmentRate));

        protected static readonly DependencyProperty ImageUnavilableProperty = DependencyProperty.Register("ImageUnavailable", typeof(Visibility), typeof(GetEquipmentRate));
        
        public GetEquipmentRate()
        {
            InitializeComponent();

            ListBoxDates.SelectionChanged += ListBoxDates_SelectionChanged;
        }

        public double DefaultRate
        {
            get { return NumericDefaultValue.Value.Value; }
        }

        public Shapefile ShapeFile
        {
            get { return _equipmentRateChoosen; }
        }

        private void ListBoxDates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EquipmentRateFile equipmentRate = ListBoxDates.SelectedItem as EquipmentRateFile;
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(equipmentRate.OverviewImage.ToArray());
            image.EndInit();
            SetValue(ImageUnavilableProperty, Visibility.Collapsed);
            SetValue(ImageProperty, image);
        }

        private void ButtonChoose_Click(object sender, RoutedEventArgs e)
        {
            EquipmentRateFile equipmentRate = ListBoxDates.SelectedItem as EquipmentRateFile;

            if (equipmentRate != null)
            {
                FileInfo execFile = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string folderPath = execFile.DirectoryName + @"\RateFile\";
                Directory.CreateDirectory(folderPath);
                string fileName = String.Empty;
                
                using (var zip = new ZipArchive(new MemoryStream(equipmentRate.ShapeZipFile.ToArray()), ZipArchiveMode.Read))
                {
                    foreach(var entry in zip.Entries)
                    {
                        if (entry.Name.Contains(".shp"))
                            fileName = entry.Name;
                        using (FileStream fileStream = new FileStream(folderPath + entry.Name, FileMode.Create))
                        {
                            Stream file = entry.Open();
                            file.CopyTo(fileStream);
                            fileStream.Flush();
                        }
                    }
                }

                _equipmentRateChoosen = Shapefile.OpenFile(folderPath + fileName);
                _equipmentRateChoosen.Projection = DotSpatial.Projections.KnownCoordinateSystems.Geographic.World.WGS1984;

                if (SettingChanged != null)
                    SettingChanged.Invoke(this, EQUIPMENTRATE_CHOOSEN);
            }
        }

        #region IDatabaseSettings

        public void RegisterDatabaseHandler(DatabaseHandler databaseHandler)
        {
            if ((_database != null && ListBoxDates.Items.Count > 0) || !_fieldChoosen.HasValue || !_equipmentChoosen.HasValue)
                return;
            _database = databaseHandler;

            _equipmentRates = databaseHandler.GetEquipmentRate(_fieldChoosen.Value, _equipmentChoosen.Value);

            if (_equipmentRates != null)
                foreach (EquipmentRateFile equipmentRate in _equipmentRates)
                    ListBoxDates.Items.Add(equipmentRate);
        }

        public Shapefile FieldRate
        {
            get { return _equipmentRateChoosen; }
        }
        
        #endregion

        #region ISettingsChanged

        public event EventHandler<string> SettingChanged;
        
        public void RegisterSettingEvent(ISettingsChanged settings)
        {
            settings.SettingChanged += Settings_SettingChanged;
        }

        private void Settings_SettingChanged(object sender, string e)
        {
            if (sender is GetField && e == GetField.FIELD_CHOOSEN)
                _fieldChoosen = (sender as GetField).FieldId;
            else if (sender is GetVechileEquipment && e == GetVechileEquipment.VECHILE_EQUIPMENT_CHOOSEN)
                _equipmentChoosen = (sender as GetVechileEquipment).Equipment.EquipmentId;
        }

        #endregion

    }
}
