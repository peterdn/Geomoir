using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Geomoir.Bluetooth;
using Geomoir.Data;
using Geomoir.Models;
using Geomoir_Tracker_Lib;
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs Args)
        {
            ReadLocations();

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

        private void ReadLocations()
        {
            Location[] results;

            using (var db = new SQLiteConnection(Database.DatabasePath))
            {
                results = db.Table<Location>().OrderByDescending(x => x.Timestamp).Take(20).ToArray();
            }

            foreach (var location in results)
            {
                var textBlock = new TextBlock();
                textBlock.Text = string.Format("{0}: {1}, {2}", 
                    location.Timestamp.FromUnixTimestampMS(), 
                    Math.Round(location.Latitude, 3), Math.Round(location.Longitude, 3));
                LocationPanel.Children.Add(textBlock);
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