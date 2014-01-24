using System.Runtime.Serialization;

namespace Geomoir.Models
{
    [DataContract]
    public class Location
    {
        [SQLite.PrimaryKey]
        [SQLite.AutoIncrement]
        [DataMember(IsRequired = true)]
        public int Id { get; set; }

        [SQLite.Unique]
        [DataMember(IsRequired = true)]
        public long Timestamp { get; set; }

        [DataMember(IsRequired = true)]
        public double Latitude { get; set; }

        [DataMember(IsRequired = true)]
        public double Longitude { get; set; }
        
        [DataMember(IsRequired = true)]
        public int Accuracy { get; set; }

        [DataMember(IsRequired = true)]
        public int CountryId { get; set; }
    }
}
