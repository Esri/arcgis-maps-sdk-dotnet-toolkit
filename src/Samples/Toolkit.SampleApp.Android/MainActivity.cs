using Android.App;
using Android.Widget;
using Android.OS;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Mapping;
using System;
using Esri.ArcGISRuntime.Mapping.Popups;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Data;
using System.Linq;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Security;
using System.IO;
using Android.Views;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    [Activity(Label = "Toolkit Samples (Native Android)", MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
    public class MainActivity : ListActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);
            ListAdapter = new SampleScreenAdapter(this, SampleDatasource.Current);
        }

        protected override void OnListItemClick(ListView l, View v, int position, long id)
        {
            var item = ((SampleScreenAdapter)ListAdapter)[position];
            StartActivity(item.Activity);
        }      
    }
}

