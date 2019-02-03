using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Json;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Maps;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace RouteMap
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.textError.Text = "";
            this.textError.Foreground = new SolidColorBrush(Colors.Red);
            this.Mapa.Center = new Geopoint(new BasicGeoposition { Latitude = 40.416946, Longitude = -3.703520 });
            this.Mapa.ZoomLevel = 15;
        }

        private async void getJson(object sender, RoutedEventArgs e)
        {
            this.textError.Text = "";
            MapService.ServiceToken = "An6jjlTc8XcKjbHJlVFiKlA6hAjJ9fjpehAR73y9pVtW9ldcu06EyU70hqcBa_rZ";
            if (this.Mapa.Routes.Count > 0)
            {
                this.Mapa.Routes.Clear();
            }            
            var httpClient = new HttpClient();
            String origen = this.origen.Text;            
            String destino = this.destino.Text;
            Uri uri = new Uri("http://dev.virtualearth.net/REST/V1/Routes/Driving?wp.0=" + origen + "&wp.1="+ destino + "&avoid=minimizeTolls&output=json&key=An6jjlTc8XcKjbHJlVFiKlA6hAjJ9fjpehAR73y9pVtW9ldcu06EyU70hqcBa_rZ");
            String jsonString = await httpClient.GetStringAsync(uri);
            Debug.WriteLine(jsonString);
            JsonValue jsonValue = JsonValue.Parse(jsonString);
            List<double> lat = new List<double>();
            List<double> lon = new List<double>();
            JsonArray itineraryItems = jsonValue.GetObject().GetNamedArray("resourceSets").GetObjectAt(0).GetNamedArray("resources").GetObjectAt(0).GetNamedArray("routeLegs").GetObjectAt(0).GetNamedArray("itineraryItems");
            foreach (JsonValue jv in itineraryItems)
            {
                lat.Add(jv.GetObject().GetNamedObject("maneuverPoint").GetNamedArray("coordinates").GetNumberAt(0));
                lon.Add(jv.GetObject().GetNamedObject("maneuverPoint").GetNamedArray("coordinates").GetNumberAt(1));
            }
            double[] latArray = lat.ToArray();
            double[] lonArray = lon.ToArray();
            List<Geopoint> wayPoints = new List<Geopoint>();
            for (int i = 0; i < latArray.Length; i++)
            {
                Debug.WriteLine(latArray[i].ToString() + ", " + lonArray[i].ToString());
                wayPoints.Add(new Geopoint(new BasicGeoposition { Latitude = latArray[i], Longitude = lonArray[i] }));
            }

            MapRouteFinderResult routeResult = await MapRouteFinder.GetDrivingRouteFromWaypointsAsync(wayPoints);

            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                // Use the route to initialize a MapRouteView.
                MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                viewOfRoute.RouteColor = Colors.Red;
                viewOfRoute.OutlineColor = Colors.Blue;

                // Add the new MapRouteView to the Routes collection
                // of the MapControl.
                this.Mapa.Routes.Add(viewOfRoute);

                // Fit the MapControl to the route.
                await this.Mapa.TrySetViewBoundsAsync(
                routeResult.Route.BoundingBox,
                null,
                Windows.UI.Xaml.Controls.Maps.MapAnimationKind.None);
            } else
            {
                this.textError.Text = "No se ha encontrado la ruta";
            }
        }

        private void Mapa_MapTapped(MapControl sender, MapInputEventArgs args)
        {
            BasicGeoposition position = args.Location.Position;
            String coordinates = position.Latitude.ToString() + ", " + position.Longitude.ToString();
            if (this.origen.Text == null || this.origen.Text.Equals(""))
            {
                this.origen.Text = coordinates;
            } else
            {
                this.destino.Text = coordinates;
            }
        }
    }
}
