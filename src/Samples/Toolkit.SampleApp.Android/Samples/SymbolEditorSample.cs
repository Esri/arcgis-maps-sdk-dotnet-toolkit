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
    [SampleInfoAttribute(Category = "SymbolDisplay", Description = "Dynamically edit a symbol and render it")]
    [Activity(Label = "Symbol Editor", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    public class SymbolEditorSample : Activity
    {
        private SimpleMarkerSymbol Symbol { get; } = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Red, 20) { Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 2) };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SymbolEditorSample);
            var display = FindViewById<SymbolDisplay>(Resource.Id.symbolDisplay);
            display.Symbol = Symbol;
            var sizeSlider = FindViewById<SeekBar>(Resource.Id.sizeSlider);
            var angleSlider = FindViewById<SeekBar>(Resource.Id.angleSlider);
            sizeSlider.ProgressChanged += (s, e) => Symbol.Size = e.Progress;
            angleSlider.ProgressChanged += (s, e) => Symbol.Angle = e.Progress;

            var styles = Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).OfType<SimpleMarkerSymbolStyle>().Select(t=>t.ToString()).ToArray();

            var styleSelector = FindViewById<Spinner>(Resource.Id.styleSelector);
            var adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerItem, styles);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            styleSelector.Adapter = adapter;
            styleSelector.ItemSelected += (s, e) =>
            {
                if(Enum.TryParse((e.View as TextView)?.Text, out SimpleMarkerSymbolStyle style))
                {
                    Symbol.Style = style;
                }
            };
        }

    }
}