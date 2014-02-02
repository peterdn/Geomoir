using System.IO;
using Geomoir.Models;
using SQLite;

namespace Geomoir_Tracker_Lib
{
    public class Database
    {
        public static string DatabasePath
        {
            get
            {
                return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "locations.db");
            }
        }

        public static void InsertLocation(Location Location)
        {
            using (var db = new SQLiteConnection(DatabasePath))
            {
                db.Insert(Location);
            }
        }
    }
}
