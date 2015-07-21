using System;


namespace FarmingGPSLib.Vechile
{
    public delegate void PosistionUpdateDelegate(object sender, PositionUpdateEventArgs e);

    interface IVechile
    {
        event PosistionUpdateDelegate PositionUpdate;
    }
}
