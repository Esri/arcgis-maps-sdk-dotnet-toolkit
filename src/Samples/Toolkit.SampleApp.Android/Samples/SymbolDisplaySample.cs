using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples
{
    [SampleInfoAttribute(Category = "SymbolDisplay", Description = "Renders a symbol")]
    [Activity(Label = "SymbolDisplay", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    public class SymbolDisplaySample : Activity
    {
        private GridLayout rootLayout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            rootLayout = new GridLayout(this);
            rootLayout.LayoutParameters = new ViewGroup.LayoutParams(-1, -1);
            rootLayout.ColumnCount = 4;
            rootLayout.RowCount = 4;
            SetContentView(rootLayout);
            LoadSymbols();
        }

        private void LoadSymbols()
        {
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 10));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 20));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 30));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.White, 40));

            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Red, 10));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Green, 20));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Blue, 30));
            AddSymbol(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Blue, 40));

            AddSymbol(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Orange, 5));
            AddSymbol(new SimpleLineSymbol(SimpleLineSymbolStyle.DashDot, System.Drawing.Color.Orange, 2));
            AddSymbol(new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Orange, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.White, 2)));
            AddSymbol(new SimpleFillSymbol(SimpleFillSymbolStyle.ForwardDiagonal, System.Drawing.Color.Yellow, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Yellow, 2)));

            AddSymbol(new PictureMarkerSymbol(new Uri("https://cdn3.iconfinder.com/data/icons/web-and-internet-icons/512/Information-256.png")));
        }

        private void AddSymbol(Symbol symbol)
        {
            var sd = new SymbolDisplay(this) { Symbol = symbol };
            sd.SetMaxHeight(40);
            var p = new GridLayout.LayoutParams();
            p.SetGravity(GravityFlags.Center);
            sd.LayoutParameters = p;
            rootLayout.AddView(sd);
        }
    }
}