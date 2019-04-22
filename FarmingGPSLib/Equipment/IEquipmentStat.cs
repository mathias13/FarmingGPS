using System;

namespace FarmingGPSLib.Equipment
{
    public interface IEquipmentStat
    {
        void ResetTotal();
        
        void AddedContent(double content);

        double Content { get; }

        double ContentLeft { get; }      

        double TotalInput { get; }
        
        event EventHandler StatUpdated;
    }
}
