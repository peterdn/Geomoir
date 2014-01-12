namespace Geomoir.Models
{
    public class Location
    {
        [SQLite.PrimaryKey]
        [SQLite.AutoIncrement]
        public int Id { get; set; }

        [SQLite.Unique]
        public long Timestamp { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Accuracy { get; set; }
    }
}
