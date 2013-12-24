using System;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using Bing.Maps;

namespace Longitude
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker {SuggestedStartLocation = PickerLocationId.Desktop};
            picker.FileTypeFilter.Add(".json");
            var storageFile = await picker.PickSingleFileAsync();
            if (storageFile == null)
            {
                return;
            }

            importProgressBar.Visibility = Visibility.Visible;
            importButton.IsEnabled = false;
            importProgressText.Text = "Loading...";

            var text = await FileIO.ReadTextAsync(storageFile);
            JsonObject jsonObject;
            if (!JsonObject.TryParse(text, out jsonObject))
            {
                // TODO: error message
                return;
            }

            var locations = jsonObject["locations"].GetArray();
            var line = new MapPolyline();

            for (var i = 0; i < locations.Count; i += 20)
            {                
                var location = locations[i].GetObject();
                var latitude = location["latitudeE7"].GetNumber() / 1e7;
                var longitude = location["longitudeE7"].GetNumber() / 1e7;

                line.Locations.Add(new Location(latitude, longitude));
            }

            var shapeLayer = new MapShapeLayer();
            shapeLayer.Shapes.Add(line);

            myMap.ShapeLayers.Add(shapeLayer);

            importProgressBar.Visibility = Visibility.Collapsed;
            importProgressText.Text = "";
            importButton.IsEnabled = true;
        }
    }
}
