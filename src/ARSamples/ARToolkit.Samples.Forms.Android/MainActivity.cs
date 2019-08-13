﻿using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V4.App;

namespace ARToolkit.SampleApp.Forms.Droid
{
    [Activity(Label = "ARToolkit.SampleApp.Forms", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            global::Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new Forms.App());
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.AccessFineLocation }, 0);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted)
            {
                Toast.MakeText(this, "Location permission is needed to run some of these samples", ToastLength.Long).Show();
            }
        }
    }
}