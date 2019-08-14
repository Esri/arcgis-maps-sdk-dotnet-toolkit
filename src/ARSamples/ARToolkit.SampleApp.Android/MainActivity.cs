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
using Android.Views;

namespace ARToolkit.SampleApp
{
    [Activity(Label = "ARToolkit Samples", MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation, Theme = "@style/Theme.AppCompat")]
    public class MainActivity : ListActivity
    {
        private static SampleDatasource list = new SampleDatasource();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();

            SetContentView(Resource.Layout.activity_main);
            ListAdapter = new SampleScreenAdapter(this, list);
        }

        protected async override void OnListItemClick(ListView l, View v, int position, long id)
        {
            var item = ((SampleScreenAdapter)ListAdapter)[position];
            if(item.HasSampleData)
            {
                var downloadView = FindViewById<LinearLayout>(Resource.Id.downloadView);
                var downloadStatus = FindViewById<TextView>(Resource.Id.downloadStatus);
                var toast = Toast.MakeText(this, "Downloading sample data... ", ToastLength.Long);
                try
                {
                    await item.GetDataAsync((status) =>
                    {
                        RunOnUiThread(() =>
                        {
                            downloadView.Visibility = ViewStates.Visible;
                            downloadStatus.Text = status;
                        });
                    }, true);
                }
                catch (System.Exception ex)
                {
                    Toast.MakeText(this, "Failed to download data: " + ex.Message, ToastLength.Long).Show();
                    return;
                }
                finally
                {
                    downloadView.Visibility = ViewStates.Gone;
                }
            }
            StartActivity(item.Type);
        }
    }
}