using System;
using FarmingGPSLib.Settings;

namespace FarmingGPSLib.Equipment
{
    public interface IEquipmentControl
    {
        void Start();

        void Stop();

        void SetRate(double rate);

        void RelaySpeed(double speed);

        bool Running { get; }

        bool Connected { get; }

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

        Type ControllerType
        {
            get;
        }
        
        object RegisterController(object settings);

        event EventHandler StatusUpdate;
    }
}
