using FarmingGPS.Database;
using FarmingGPS.Settings;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FarmingGPS.Usercontrols
{
    /// <summary>
    /// Interaction logic for GetField.xaml
    /// </summary>
    public partial class GetVechileEquipment : UserControl, IDatabaseSettings, ISettingsChanged
    {
        private Vechile _vechile;

        private Equipment _equipment;

        private DatabaseHandler _database;

        public static readonly string VECHILE_EQUIPMENT_CHOOSEN = "VECHILE_EQUIPMENT_CHOOSEN";

        public GetVechileEquipment()
        {
            InitializeComponent();
        }
        
        public void ReloadLists()
        {
            ListBoxVechile.Items.Clear();
            Vechile[] vechiles = _database.GetVechiles();
            if (vechiles != null)
            {
                foreach (Vechile vechile in vechiles)
                    ListBoxVechile.Items.Add(vechile);
            }

            ListBoxEquipment.Items.Clear();
            Equipment[] equipments = _database.GetEquipments();
            if (equipments != null)
            {
                foreach (Equipment equipment in equipments)
                    ListBoxEquipment.Items.Add(equipment);
            }
        }

        #region Button Events

        private void ButtonVechileUpdate_Click(object sender, RoutedEventArgs e)
        {
            if(ListBoxVechile.SelectedItem != null)
            {
                Vechile vechile = ListBoxVechile.SelectedItem as Vechile;
                vechile.Manufacturer = TextBoxVechileManufacturer.Text;
                vechile.Model = TextBoxVechileModel.Text;
                _database.SubmitToDatabase();
                ReloadLists();
            }
        }

        private void ButtonVechileAdd_Click(object sender, RoutedEventArgs e)
        {
            Vechile vechile = new Vechile();
            vechile.Manufacturer = TextBoxVechileManufacturer.Text;
            vechile.Model = TextBoxVechileModel.Text;
            _database.AddVechile(vechile);
            _database.SubmitToDatabase();
            ReloadLists();
        }

        private void ButtonVechileDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxVechile.SelectedItem != null)
            {
                _database.DeleteVechile(ListBoxVechile.SelectedItem as Vechile);
                _database.SubmitToDatabase();
                ReloadLists();
            }
        }

        private void ButtonEquipmentUpdate_Click(object sender, RoutedEventArgs e)
        {
            if(ListBoxEquipment.SelectedItem != null)
            {
                Equipment equipment = ListBoxEquipment.SelectedItem as Equipment;
                equipment.Name = TextBoxEquipmenName.Text;
                equipment.WorkWidth = NumericEquipmentWorkWidth.Value.Value;
                equipment.AngleFromAttach = NumericEquipmentAngleFromAttach.Value.Value;
                equipment.DistFromAttach = NumericEquipmentDistFromAttach.Value.Value;
                _database.SubmitToDatabase();
                ReloadLists();
            }
        }

        private void ButtonEquipmentAdd_Click(object sender, RoutedEventArgs e)
        {
            Equipment equipment = new Equipment();
            equipment.Name = TextBoxEquipmenName.Text;
            equipment.WorkWidth = NumericEquipmentWorkWidth.Value.Value;
            equipment.AngleFromAttach = NumericEquipmentAngleFromAttach.Value.Value;
            equipment.DistFromAttach = NumericEquipmentDistFromAttach.Value.Value;
            _database.AddEquipment(equipment);
            _database.SubmitToDatabase();
            ReloadLists();
        }

        private void ButtonEquipmentDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxEquipment.SelectedItem != null)
            {
                _database.DeleteEquipment(ListBoxEquipment.SelectedItem as Equipment);
                _database.SubmitToDatabase();
                ReloadLists();
            }
        }

        private void ButtonChoose_Click(object sender, RoutedEventArgs e)
        {
            if(ListBoxEquipment.SelectedItem != null && ListBoxVechile.SelectedItem != null)
            {
                _vechile = ListBoxVechile.SelectedItem as Vechile;
                _equipment = ListBoxEquipment.SelectedItem as Equipment;
                if (SettingChanged != null)
                    SettingChanged.Invoke(this, VECHILE_EQUIPMENT_CHOOSEN);
            }
        }

        #endregion

        #region Public Properties

        public Vechile Vechile
        {
            get { return _vechile; }
        }

        public Equipment Equipment
        {
            get { return _equipment; }
        }

        #endregion

        #region IDatabaseSettings

        public void RegisterDatabaseHandler(DatabaseHandler databaseHandler)
        {
            if (_database != null)
                return;
            _database = databaseHandler;
            ReloadLists();
        }

        #endregion

        #region ISettingsChanged

        public event EventHandler<string> SettingChanged;

        #endregion

        private void ListBoxEquipment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ListBoxEquipment.SelectedItem != null)
            {
                Equipment equipment = ListBoxEquipment.SelectedItem as Equipment;
                TextBoxEquipmenName.Text = equipment.Name;
                NumericEquipmentWorkWidth.Value = equipment.WorkWidth;
                NumericEquipmentDistFromAttach.Value = equipment.DistFromAttach;
                NumericEquipmentAngleFromAttach.Value = equipment.AngleFromAttach;
            }
        }

        private void ListBoxVechile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxVechile.SelectedItem != null)
            {
                Vechile vechile = ListBoxVechile.SelectedItem as Vechile;
                TextBoxVechileManufacturer.Text = vechile.Manufacturer;
                TextBoxVechileModel.Text = vechile.Model;
            }
        }
    }
}
