using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Geolocation;
using Geomoir.Data;
using Geomoir.Models;
using Geomoir_Tracker_Lib;
using Microsoft.Phone.Scheduler;

namespace Geomoir_Tracker_Agent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected async override void OnInvoke(ScheduledTask task)
        {
            var location = await GetCurrentLocation();

            Database.InsertLocation(location);

#if DEBUG
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(60));
#endif

            NotifyComplete();
        }

        private async Task<Location> GetCurrentLocation()
        {
            var geolocater = new Geolocator();

            var geoposition = await geolocater.GetGeopositionAsync();

            var location = new Location();
            location.Latitude = geoposition.Coordinate.Latitude;
            location.Longitude = geoposition.Coordinate.Longitude;

            location.Timestamp = DateTime.Now.ToUnixTimestampMS();

            // TODO: check accuracies here mean the same thing
            location.Accuracy = (int)geoposition.Coordinate.Accuracy;

            return location;
        }
    }
}