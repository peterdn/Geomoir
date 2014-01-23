using System.Collections.Generic;
using System.IO;

namespace Geomoir.Data
{
    public static class Countries
    {
        public static List<string> LoadCountryNames(StreamReader Reader)
        {
            var countryNames = new List<string>();
            while (!Reader.EndOfStream)
                countryNames.Add(Reader.ReadLine());
            return countryNames;
        }
    }
}
