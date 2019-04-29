using System;
using System.Runtime.InteropServices;

namespace FarmingGPSLib.Equipment.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct COMMTIMEOUTS
    {
        internal UInt32 ReadIntervalTimeout;
        internal UInt32 ReadTotalTimeoutMultiplier;
        internal UInt32 ReadTotalTimeoutConstant;
        internal UInt32 WriteTotalTimeoutMultiplier;
        internal UInt32 WriteTotalTimeoutConstant;
    }
}