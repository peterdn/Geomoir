using System;

namespace Geomoir.Data
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the current DateTime expressed in milliseconds from midnight 1970-01-01.
        /// </summary>
        /// <param name="Time"></param>
        /// <returns></returns>
        public static long ToUnixTimestampMS(this DateTime Time)
        {
            return (Time - new DateTime(1970, 1, 1)).Ticks / 10000;
        }

        /// <summary>
        /// Gets the current DateTime expressed in milliseconds from midnight 1970-01-01.
        /// </summary>
        /// <param name="Time"></param>
        /// <returns></returns>
        public static DateTime FromUnixTimestampMS(this long Time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0) + new TimeSpan(Time * 10000);
        }
    }
}
