using System;

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
            // ARCore requires camera permissions to operate. If we did not yet obtain runtime
            // permission on Android M and above, now is a good time to ask the user for it.
            //TODO ArCoreApk.Instance.RequestInstall(this, )
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted)
            {
                // try
                // {
                //     //this.arView.StartTracking();
                // }
                // catch (System.Exception ex)
                // {
                //     Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                //     Finish();
                // }
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.Camera }, 0);
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera) != Android.Content.PM.Permission.Granted)
            {
                Toast.MakeText(this, "Camera permission is needed to run this application", ToastLength.Long).Show();
                Finish();
            }
            //global::Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            //
            //base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}