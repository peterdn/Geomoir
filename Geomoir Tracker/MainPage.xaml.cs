using System;
using System.Device.Location;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Geomoir.Bluetooth;
using Geomoir.Data;
using Geomoir.Models;
using Geomoir_Tracker_Lib;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Scheduler;
using SQLite;

namespace Geomoir_Tracker
{
    public partial class MainPage
    {
        private PeriodicTask _trackingTask;

        private const string _TRACKING_TASK_NAME = "TrackingAgent";

        private bool _initialized;

        private bool TrackingEnabled
        {
            set
            {
                // TODO: change this to data binding
                TrackingButton.Content = value ? "On" : "Off";
            }
        }
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs Args)
        {
            _initialized = false;
            
            var location = await GetLastLocation();

            if (location != null)
            {
                var geocoordinate = new GeoCoordinate(location.Latitude, location.Longitude);
                Map.SetView(geocoordinate, 18, MapAnimationKind.Linear);
            }

            _trackingTask = ScheduledActionService.Find(_TRACKING_TASK_NAME) as PeriodicTask;

            if (_trackingTask != null)
            {
                TrackingButton.DataContext = _trackingTask;
                TrackingEnabled = _trackingTask.IsEnabled;
            }
            else
            {
                TrackingEnabled = false;
            }

            _initialized = true;
        }

        private async Task<Location> GetLastLocation()
        {
            using (var db = new SQLiteConnection(Database.DatabasePath))
            {
                return db.Table<Location>().OrderByDescending(x => x.Timestamp).FirstOrDefault();
            }
        }

        private void StartTracking()
        {
            _trackingTask = ScheduledActionService.Find(_TRACKING_TASK_NAME) as PeriodicTask;

            if (_trackingTask != null)
                StopTracking();

            _trackingTask = new PeriodicTask(_TRACKING_TASK_NAME);

            _trackingTask.Description = "Tracks location";

            try
            {
                ScheduledActionService.Add(_trackingTask);

#if DEBUG
                ScheduledActionService.LaunchForTest(_TRACKING_TASK_NAME, TimeSpan.FromSeconds(60));
#endif

                TrackingEnabled = true;
            } 
            catch (Exception ex)
            {
                TrackingButton.IsChecked = false;
                TrackingEnabled = false;
                MessageBox.Show(ex.Message);
            }
        }

        private void StopTracking()
        {
            try
            {
                ScheduledActionService.Remove(_TRACKING_TASK_NAME);
                TrackingEnabled = false;
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void SyncBluetoothButton_Click(object Sender, RoutedEventArgs Args)
        {
            var client = new BluetoothClient();
            client.ConnectionEstablished += ClientOnConnectionEstablished;
            await client.Connect();
        }

        private async void ClientOnConnectionEstablished(BluetoothClient Client, ClientConnectedEventArgs Args)
        {
            var connection = Args.Connection;

            using (var db = new SQLiteConnection(Database.DatabasePath))
            {
                var query = db.Table<Location>().OrderBy(x => x.Timestamp).ToArray();

                // First send the number of locations we want to send.
                await connection.SendUInt32((uint)query.Length);

                foreach (var location in query)
                {
                    await connection.SendObject(location);
                }
            }

            // Disconnect
        }

        private void TrackingButton_Checked(object Sender, RoutedEventArgs Args)
        {
            if (_initialized)
                StartTracking();
        }

        private void TrackingButton_Unchecked(object Sender, RoutedEventArgs Args)
        {
            if (_initialized)
                StopTracking();
        }
    }
}