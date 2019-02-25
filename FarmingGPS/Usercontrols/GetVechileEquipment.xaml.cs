using FarmingGPS.Database;
using FarmingGPSLib.Settings;
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

        private VechileAttach _vechileAttach;

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

            ListBoxAttach.Items.Clear();
            TextBoxAttachName.Text = string.Empty;
            TextBoxEquipmenName.Text = string.Empty;
            TextBoxEquipmenClass.Text = string.Empty;
            TextBoxVechileManufacturer.Text = string.Empty;
            TextBoxVechileModel.Text = string.Empty;
            NumericAttachAngleFromCenter.Value = null;
            NumericAttachDistFromCenter.Value = null;
            NumericEquipmentAngleFromAttach.Value = null;
            NumericEquipmentDistFromAttach.Value = null;
            NumericEquipmentWorkWidth.Value = null;
            NumericVechileReceiverAngleToCenter.Value = null;
            NumericVechileReceiverDistToCenter.Value = null;
        }

        #region Button Events

        private void ButtonVechileUpdate_Click(object sender, RoutedEventArgs e)
        {
            if(ListBoxVechile.SelectedItem != null)
            {
                Vechile vechile = ListBoxVechile.SelectedItem as Vechile;
                vechile.Manufacturer = TextBoxVechileManufacturer.Text;
                vechile.Model = TextBoxVechileModel.Text;
                vechile.ReceiverAngleFromCenter = (float)NumericVechileReceiverAngleToCenter.Value.Value;
                vechile.ReceiverDistFromCenter = (float)NumericVechileReceiverDistToCenter.Value.Value;
                _database.SubmitToDatabase();
                ReloadLists();
            }
        }

        private void ButtonVechileAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!NumericVechileReceiverAngleToCenter.Value.HasValue && !NumericVechileReceiverDistToCenter.Value.HasValue)
                return;
            Vechile vechile = new Vechile();
            vechile.Manufacturer = TextBoxVechileManufacturer.Text;
            vechile.Model = TextBoxVechileModel.Text;
            vechile.ReceiverAngleFromCenter = (float)NumericVechileReceiverAngleToCenter.Value.Value;
            vechile.ReceiverDistFromCenter = (float)NumericVechileReceiverDistToCenter.Value.Value;
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
        
        private void ButtonAttachUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxAttach.SelectedItem != null)
            {
                VechileAttach attach = ListBoxAttach.SelectedItem as VechileAttach;
                attach.Name = TextBoxAttachName.Text;
                attach.AttachAngleFromCenter = (float)NumericAttachAngleFromCenter.Value.Value;
                attach.AttachDistFromCenter = (float)NumericAttachDistFromCenter.Value.Value;
                _database.SubmitToDatabase();
                ReloadLists();
            }
        }

        private void ButtonAttachAdd_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxVechile.SelectedItem == null || !NumericAttachAngleFromCenter.Value.HasValue && !NumericAttachDistFromCenter.Value.HasValue)
                return;
            VechileAttach attach = new VechileAttach();
            attach.VechileId = (ListBoxVechile.SelectedItem as Vechile).VechileId;
            attach.Name = TextBoxAttachName.Text;
            attach.AttachAngleFromCenter = (float)NumericAttachAngleFromCenter.Value.Value;
            attach.AttachDistFromCenter = (float)NumericAttachDistFromCenter.Value.Value;
            _database.AddVechileAttach(attach);
            _database.SubmitToDatabase();
            ReloadLists();
        }

        private void ButtonAttachDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxAttach.SelectedItem != null)
            {
                _database.DeleteVechileAttach(ListBoxAttach.SelectedItem as VechileAttach);
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
                equipment.EquipmentClass = TextBoxEquipmenClass.Text;
                _database.SubmitToDatabase();
                ReloadLists();
            }
        }

        private void ButtonEquipmentAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!NumericEquipmentAngleFromAttach.Value.HasValue || !NumericEquipmentDistFromAttach.Value.HasValue || !NumericEquipmentWorkWidth.Value.HasValue)
                return;
            Equipment equipment = new Equipment();
            equipment.Name = TextBoxEquipmenName.Text;
            equipment.WorkWidth = NumericEquipmentWorkWidth.Value.Value;
            equipment.AngleFromAttach = NumericEquipmentAngleFromAttach.Value.Value;
            equipment.DistFromAttach = NumericEquipmentDistFromAttach.Value.Value;
            equipment.EquipmentClass = TextBoxEquipmenClass.Text;
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
            if(ListBoxEquipment.SelectedItem != null && ListBoxVechile.SelectedItem != null && ListBoxAttach.SelectedItem != null)
            {
                _vechile = ListBoxVechile.SelectedItem as Vechile;
                _vechileAttach = ListBoxAttach.SelectedItem as VechileAttach;
                _equipment = ListBoxEquipment.SelectedItem as Equipment;
                if (SettingChanged != null)
                    SettingChanged.Invoke(this, VECHILE_EQUIPMENT_CHOOSEN);
            }
            //TODO prompt that you have to choose
        }

        #endregion

        #region Public Properties

        public Vechile Vechile
        {
            get { return _vechile; }
        }

        public VechileAttach VechileAttach
        {
            get { return _vechileAttach; }
        }

        public Equipment Equipment
        {
            get { return _equipment; }
        }
        
        #endregion

        #region IDatabaseSettings

        public void RegisterDatabaseHandler(DatabaseHandler databaseHandler)
        {
            if (_database != null && ListBoxVechile.Items.Count > 0)
                return;
            _database = databaseHandler;
            ReloadLists();
        }

        #endregion

        #region ISettingsChanged

        public event EventHandler<string> SettingChanged;

        public void RegisterSettingEvent(ISettingsChanged settings)
        {
            throw new NotImplementedException();
        }

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
                TextBoxEquipmenClass.Text = equipment.EquipmentClass;
            }
        }

        private void ListBoxVechile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxVechile.SelectedItem != null)
            {
                Vechile vechile = ListBoxVechile.SelectedItem as Vechile;
                TextBoxVechileManufacturer.Text = vechile.Manufacturer;
                TextBoxVechileModel.Text = vechile.Model;
                NumericVechileReceiverAngleToCenter.Value = vechile.ReceiverAngleFromCenter;
                NumericVechileReceiverDistToCenter.Value = vechile.ReceiverDistFromCenter;

                VechileAttach[] attachPoints = _database.GetAttachPoints(vechile.VechileId);
                ListBoxAttach.Items.Clear();
                if (attachPoints != null)
                {
                    foreach (VechileAttach attachPoint in attachPoints)
                        ListBoxAttach.Items.Add(attachPoint);
                }
            }
        }

        private void ListBoxAttach_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxAttach.SelectedItem != null)
            {
                VechileAttach vechileAttach = ListBoxAttach.SelectedItem as VechileAttach;
                TextBoxAttachName.Text = vechileAttach.Name;
                NumericAttachAngleFromCenter.Value = vechileAttach.AttachAngleFromCenter;
                NumericAttachDistFromCenter.Value = vechileAttach.AttachDistFromCenter;
            }
        }
    }
}
