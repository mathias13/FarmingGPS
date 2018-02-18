using System;

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
    }
}
