// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Bing.Maps;
using SQLite;

namespace Geomoir
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private BluetoothServer _bluetoothServer;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ButtonBase_OnClick(object Sender, RoutedEventArgs E)
        {
            if (Sender == ImportAppBarButton)
            {
                Import();
            }
            else if (Sender == StartBluetoothAppBarButton)
            {
                StartBluetooth();
            }
        }

        private async void StartBluetooth()
        {
            _bluetoothServer = new BluetoothServer();
            _bluetoothServer.Start();
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

        private void AddLocationsToDatabase(SQLiteConnection db, JsonArray locations, int start, int count, int step)
        {
            for (var i = start; i < start + count && i < locations.Count; i += step)
            {
                var location = locations[i].GetObject();
                var latitude = location["latitudeE7"].GetNumber() / 1e7;
                var longitude = location["longitudeE7"].GetNumber() / 1e7;
                var timestampMs = long.Parse(location["timestampMs"].GetString());
                var accuracy = (int)location["accuracy"].GetNumber();

                db.Insert(new Models.Location { Latitude = latitude, Longitude = longitude, Timestamp = timestampMs, Accuracy = accuracy });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var app = (App) Application.Current;
            using (var db = new SQLiteConnection(app.DatabasePath))
            {
                var query = db.Table<Models.Location>().OrderBy(x => x.Timestamp).ToArray();
                var line = new MapPolyline();
                var step = 20;
                for (var i = 0; i < query.Length; i += step)
                {
                    var location = query[i];
                    line.Locations.Add(new Location(location.Latitude, location.Longitude));
                }

                var shapeLayer = new MapShapeLayer();
                shapeLayer.Shapes.Add(line);

                myMap.ShapeLayers.Add(shapeLayer);
            }
        }
    }
}
