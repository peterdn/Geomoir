using System;
using System.Device.Location;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Windows.Devices.Geolocation;
using Geomoir.Bluetooth;
using Geomoir.Models;
using SQLite;

namespace Geomoir_Tracker
{
    public partial class MainPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            
            var app = (App)Application.Current;

            databasePathTextBlock.Text = app.DatabasePath;
        }

        private async void AddLocationButton_Click(object Sender, RoutedEventArgs Args)
        {
            var app = (App)Application.Current;

            var geolocater = new Geolocator();

            var geoposition = await geolocater.GetGeopositionAsync();

            var location = new Location();
            location.Latitude = geoposition.Coordinate.Latitude;
            location.Longitude = geoposition.Coordinate.Longitude;

            location.Timestamp = DateTime.Now.ToUnixTimestampMS();

            // TODO: check accuracies here mean the same thing
            location.Accuracy = (int)geoposition.Coordinate.Accuracy;

            using (var db = new SQLiteConnection(app.DatabasePath))
            {
                db.Insert(location);
            }
        }

        private void readLocationsButton_Click(object Sender, RoutedEventArgs Args)
        {
            var app = (App)Application.Current;

            using (var db = new SQLiteConnection(app.DatabasePath))
            {
                var query = db.Table<Location>().OrderBy(x => x.Timestamp).ToArray();

                databasePathTextBlock.Text = string.Format("Got {0} locations!", query.Length);
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
            var app = (App)Application.Current;
            var connection = Args.Connection;

            using (var db = new SQLiteConnection(app.DatabasePath))
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
    }
}