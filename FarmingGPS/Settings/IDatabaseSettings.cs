using System;
using FarmingGPS.Database;

namespace FarmingGPS.Settings
{
    interface IDatabaseSettings
    {
        void RegisterDatabaseHandler(DatabaseHandler databaseHandler);
    }
}
