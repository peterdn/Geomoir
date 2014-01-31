// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Bing.Maps;
using Geomoir.Bluetooth;
using Geomoir.Data;
using SQLite;

namespace Geomoir
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        public static readonly Guid ServiceGUID = Guid.Parse("EEB35C6F-33DB-4DE3-8B4E-CFA313E92640");

        private readonly BluetoothServer _bluetoothServer;

        private DuplexConnection _connection;

        private QuadTree<byte> _quadtree;

        private List<string> _countries;

        public MainPage()
        {
            this.InitializeComponent();
            _bluetoothServer = new BluetoothServer(ServiceGUID);
            _bluetoothServer.StateChanged += BluetoothServerOnStateChanged;
            _bluetoothServer.ClientConnected += BluetoothServerOnClientConnected;
            _bluetoothServer.ConnectionError += BluetoothServerOnConnectionError;
            LoadCountriesAndQuadTree();
        }

        private void BluetoothServerOnConnectionError(BluetoothServer Sender, Exception Args)
        {
            SafeInvoke(() => {
                LogTextBlock.Text = "Error: " + Args.Message;
            });
        }

        private async void BluetoothServerOnClientConnected(BluetoothServer Server, ClientConnectedEventArgs Args)
        {
            var device = Args.Device;

            _connection = Args.Connection;

            SafeInvoke(() => {
                LogTextBlock.Text = string.Format("Connected to {0} hostname {1} service {2}", device.DisplayName, device.HostName, device.ServiceName);
            });

            var connection = Args.Connection;
            
            // The number of locations to sync
            var count = await connection.ReceiveUInt32();
            
            var app = (App)Application.Current;
            using (var db = new SQLiteConnection(app.DatabasePath))
            {
                for (int i = 0; i < count; ++i)
                {
                    var location = await connection.ReceiveObject<Models.Location>();
                    location.CountryId = _quadtree.Query(new Coordinate((float) location.Longitude, (float) location.Latitude));
                    db.Insert(location);
                }
            }

            SafeInvoke(() =>
            {
                LogTextBlock.Text += string.Format("\n\rSynced {0} locations!", count);
            });
        }

        private void BluetoothServerOnStateChanged(BluetoothServer Server, BluetoothServer.BluetoothServerState ServerState)
        {
            SafeInvoke(() => {
                ServerStatusTextBlock.Text = ServerState.ToString();
                if (ServerState == BluetoothServer.BluetoothServerState.Advertising || ServerState == BluetoothServer.BluetoothServerState.Connected)
                {
                    ToggleServerButton.Content = "Start Bluetooth server";
                }
                else
                {
                    ToggleServerButton.Content = "Stop Bluetooth server";
                }
            });
        }

        private void ButtonBase_OnClick(object Sender, RoutedEventArgs E)
        {
            if (Sender == ImportAppBarButton)
            {
                Import();
            }
            else if (Sender == StartBluetoothAppBarButton)
            {
                //StartBluetooth();
            }
        }

        private async void Import()
        {
            var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Desktop };
            picker.FileTypeFilter.Add(".json");
            var storageFile = await picker.PickSingleFileAsync();
            if (storageFile == null)
            {
                return;
            }

            importProgressBar.Visibility = Visibility.Visible;
            importProgressText.Text = "Loading...";

            var text = await FileIO.ReadTextAsync(storageFile);
            JsonObject jsonObject;
            if (!JsonObject.TryParse(text, out jsonObject))
            {
                // TODO: error message
                return;
            }

            var locations = jsonObject["locations"].GetArray();

            importProgressBar.Maximum = locations.Count;
            importProgressBar.Value = 0;

            var app = (App)Application.Current;
            using (var db = new SQLiteConnection(app.DatabasePath))
            {
                var step = 1;
                var count = 1000;
                for (var i = 0; i < locations.Count; i += step * count)
                {
                    db.BeginTransaction();
                    var i1 = i;
                    await Task.Run(() => AddLocationsToDatabase(db, locations, i1, count, step));
                    db.Commit();
                    importProgressText.Text = string.Format("Imported {0} of {1}", i, locations.Count);
                    importProgressBar.Value += step * count;
                }
            }

            importProgressBar.Visibility = Visibility.Collapsed;
            importProgressText.Text = "";
        }

        private async void LoadCountriesAndQuadTree()
        {
            // Load dictionary and quad tree for country lookup.
            var countriesFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Data/countries.dat"));
            var countriesStream = await countriesFile.OpenAsync(FileAccessMode.Read);
            _countries = Countries.LoadCountryNames(new StreamReader(countriesStream.AsStream()));
            countriesStream.Dispose();

            var quadtreeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Data/quadtree.dat"));
            var quadtreeStream = await quadtreeFile.OpenAsync(FileAccessMode.Read);
            _quadtree = Serializer.LoadQuadTree(new BinaryReader(quadtreeStream.AsStream()));
            quadtreeStream.Dispose();
        }

        private void AddLocationsToDatabase(SQLiteConnection db, JsonArray locations, int start, int count, int step)
        {
            for (var i = start; i < start + count && i < locations.Count; i += step)
            {
                var location = locations[i].GetObject();
                var latitude = location["latitudeE7"].GetNumber() / 1e7;
                var longitude = location["longitudeE7"].GetNumber() / 1e7;
                var timestampMs = long.Parse(location["timestampMs"].GetString());
                var accuracy = (int)location["accuracy"].GetNumber();

                var countryId = _quadtree.Query(new Coordinate((float)longitude, (float)latitude));

                db.Insert(new Models.Location
                {
                    Latitude = latitude, 
                    Longitude = longitude, 
                    Timestamp = timestampMs, 
                    Accuracy = accuracy,
                    CountryId = countryId
                });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private async void ToggleServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_bluetoothServer.ServerState == BluetoothServer.BluetoothServerState.Connected || _bluetoothServer.ServerState == BluetoothServer.BluetoothServerState.Advertising)
            {
                _bluetoothServer.Stop();
            }
            else
            {
                await _bluetoothServer.Start();
            }
        }

        public async static void SafeInvoke(Action Invokee)
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                Invokee();
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Invokee());
            }
        }

        private void PlotLocationsButton_Click(object Sender, RoutedEventArgs Args)
        {
            var app = (App)Application.Current;
            using (var db = new SQLiteConnection(app.DatabasePath))
            {
                var query = db.Table<Models.Location>().OrderBy(x => x.Timestamp).ToArray();
                var firstLocation = query.First();

                var location = new Location(firstLocation.Latitude, firstLocation.Longitude);

                var pushpin = new Pushpin();

                myMap.Children.Add(pushpin);

                MapLayer.SetPosition(pushpin, location);

                myMap.SetView(location, 10);
            }
        }
    }
}
