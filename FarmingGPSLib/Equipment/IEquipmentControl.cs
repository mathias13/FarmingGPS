using System;
using FarmingGPSLib.Settings;

namespace FarmingGPSLib.Equipment
{
    public interface IEquipmentControl
    {
        void Start();

        void Stop();

        double StartDistance
        {
            get;
        }

        double StopDistance
        {
            get;
        }

        Type ControllerSettingsType
        {
            get;
        }
        
        void RegisterController(object settings);
    }
}
