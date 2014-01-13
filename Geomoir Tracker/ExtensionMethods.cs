using System;

namespace Geomoir_Tracker
{
    static class ExtensionMethods
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
    }
}
