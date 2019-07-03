using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Mapping;
using Android.Opengl;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using System;
using Javax.Microedition.Khronos.Egl;
using Javax.Microedition.Khronos.Opengles;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Android.Support.Design.Widget;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Geometry;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Globalization;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Symbology;

namespace ARToolkit.SampleApp.Samples
{
    [Activity(
        Label = "Satellites",
        Theme = "@style/Theme.AppCompat.NoActionBar",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class SatellitesSample : ARActivityBase
    {
        private Scene Scene;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                Scene = new Scene(Basemap.CreateImagery());
                Scene.BaseSurface = new Surface();
                Scene.BaseSurface.BackgroundGrid.IsVisible = false;
                Scene.BaseSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("http://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
                Scene.BaseSurface.ElevationExaggeration = 10;
                Scene.BaseSurface.NavigationConstraint = NavigationConstraint.None;
                ARView.TranslationFactor = 100000000;
                // Set pitch to 0 so looking forward looks "down" on earth from space
                ARView.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(0, 0, 20000000, SpatialReferences.Wgs84), 0, 0, 0);
                await Scene.LoadAsync();
                ARView.Scene = Scene;
                var go = await LoadSatellites();
                ARView.GraphicsOverlays.Add(go);
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(this, "Failed to load scene: \n" + ex.Message, ToastLength.Long).Show();
            }
        }

        private async Task<GraphicsOverlay> LoadSatellites()
        {
            GraphicsOverlay go = new GraphicsOverlay();
            go.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            go.Renderer = new SimpleRenderer(new SimpleMarkerSymbol() { Size = 4 });
            var sats = await SatelliteAPI.GetSatellitesAsync3().ConfigureAwait(false);
            var now = new One_Sgp4.EpochTime(DateTime.UtcNow);
            foreach (var tle in sats)
            {
                try
                {
                    var pos = One_Sgp4.SatFunctions.getSatPositionAtTime(tle, now, One_Sgp4.Sgp4.wgsConstant.WGS_84);
                    var ssp = One_Sgp4.SatFunctions.calcSatSubPoint(now, pos, One_Sgp4.Sgp4.wgsConstant.WGS_84);
                    var p = new MapPoint(ssp.getLongitude(), ssp.getLatetude(), ssp.getHeight() * 1000, SpatialReferences.Wgs84);
                    Graphic g = new Graphic(p);
                    go.Graphics.Add(g);
                }
                catch { }
            }
            return go;
        }

        internal class SatelliteAPI
        {
            const string url = "https://maps.esri.com/rc/sat2/data/tle.20171129.txt";
            public static async Task<List<One_Sgp4.Tle>> GetSatellitesAsync3()
            {
                System.Net.Http.HttpClient c = new System.Net.Http.HttpClient();
                var result = new List<One_Sgp4.Tle>();

                var tledata = await c.GetStreamAsync(url).ConfigureAwait(false);
                using (var sr = new StreamReader(tledata))
                {
                    while (!sr.EndOfStream)
                    {
                        var line1 = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line1))
                            continue;
                        var line2 = sr.ReadLine();
                        var s = One_Sgp4.ParserTLE.parseTle(line1, line2);
                        result.Add(s);
                    }
                }
                return result;
            }
        }
        internal class SatelliteTle
        {
            public SatelliteTle(string line1, string line2)
            {
                CatalogNumber = line1.Substring(2, 5).Trim();
                Classification = line1[7];
                InternationalDesignator = line1.Substring(9, 8).Trim();
                EpochYear = int.Parse(line1.Substring(18, 2), CultureInfo.InvariantCulture);
                EpochDay = double.Parse(line1.Substring(20, 12), CultureInfo.InvariantCulture);
                FirstDerivativeOfTheMeanMotion = double.Parse(line1.Substring(33, 10), CultureInfo.InvariantCulture);
                SecondDerivativeOfTheMeanMotion = line1.Substring(44, 8);
                BStarDragTerm = line1.Substring(53, 8);
                ElementSetType = line1[62];
                ElementNumber = int.Parse(line1.Substring(64, 5), CultureInfo.InvariantCulture);

                OrbitInclination = double.Parse(line2.Substring(8, 8), CultureInfo.InvariantCulture);
                RightAscensionOfAscendingNode = double.Parse(line2.Substring(17, 8), CultureInfo.InvariantCulture);
                Eccentricity = double.Parse(line2.Substring(26, 8), CultureInfo.InvariantCulture);
                ArgumentOfPerigee = double.Parse(line2.Substring(34, 8), CultureInfo.InvariantCulture);
                MeanAnomaly = double.Parse(line2.Substring(43, 8), CultureInfo.InvariantCulture);
                MeanMotion = double.Parse(line2.Substring(52, 8), CultureInfo.InvariantCulture);
                RevolutionNumberAtEpoch = int.Parse(line2.Substring(63, 5), CultureInfo.InvariantCulture);
            }
            /// <summary>
            /// Satellite Catalog Number
            /// </summary>
            /// <remarks>
            /// The catalog number assigned to the object by the US Air Force.
            /// Numbers are assigned sequentially as objects are cataloged.
            /// Object numbers less than 10000 are always aligned to the right, and padded with zeros or spaces to the left.
            /// </remarks>
            public string CatalogNumber { get; }
            /// <summary>
            /// The security classification of the element set. All objects on this site will have a classification of 'U' (unclassified).
            /// </summary>
            public char Classification { get; }
            /// <summary>
            /// International Designator
            /// </summary>
            /// <remarks>
            /// This is another format for identifying an object.
            /// - The first two characters designate the launch year of the object.
            /// - The next 3 characters indicate the launch number, starting from the beginning of the year. This particular launch was the 67th launch of 1998.
            /// - The remainder of the field(1 to 3 characters) indicates the piece of the launch.Piece 'A' is usually the payload.
            /// </remarks>
            public string InternationalDesignator { get; }
            /// <summary>
            /// Element Set Epoch
            /// </summary>
            /// <remarks>
            /// - The first two digits ('04') indicate the year. Add 1900 for years >= 57, and 2000 for all others.
            /// - The remainder of the field('236.56031392') is the day of the year.
            /// - Spaces or numbers are acceptable in day of the year. (e.g. '236' or '006' or ' 6').
            /// </remarks>
            public int EpochYear { get; }
            public double EpochDay { get; }
            public double FirstDerivativeOfTheMeanMotion { get; }
            public string SecondDerivativeOfTheMeanMotion { get; }
            public string BStarDragTerm { get; }
            public char ElementSetType { get; }
            public int ElementNumber { get; }
            /// <summary>
            /// Orbit Inclination (degrees)
            /// </summary>
            public double OrbitInclination { get; }
            /// <summary>
            /// Right Ascension of Ascending Node (degrees)
            /// </summary>
            public double RightAscensionOfAscendingNode { get; }
            /// <summary>
            /// Eccentricity
            /// </summary>
            public double Eccentricity { get; }
            /// <summary>
            /// Argument of Perigee (degrees)
            /// </summary>
            public double ArgumentOfPerigee { get; }
            /// <summary>
            /// Mean Anomaly (degrees)
            /// </summary>
            public double MeanAnomaly { get; }
            /// <summary>
            /// Mean Motion (revolutions/day)
            /// </summary>
            public double MeanMotion { get; }
            /// <summary>
            /// Revolution Number at Epoch
            /// </summary>
            public int RevolutionNumberAtEpoch { get; }

        }
    }
}