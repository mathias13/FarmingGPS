using System;
using FarmingGPS.Database;

namespace FarmingGPSLib.Settings
{
    public interface IDatabaseSettings
    {
        void RegisterDatabaseHandler(DatabaseHandler databaseHandler);
    }
}
