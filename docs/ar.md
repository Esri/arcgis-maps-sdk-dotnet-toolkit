# AR Toolkit

[![guide doc](https://img.shields.io/badge/Full_Developers_Guide-Doc-purple)](https://developers.arcgis.com/net/latest/forms/guide/display-scenes-in-augmented-reality.htm) [![world-scale sample](https://img.shields.io/badge/World_Scale-Sample-blue)](https://developers.arcgis.com/net/latest/ios/sample-code/collect-data-in-ar/) [![Tabletop sample](https://img.shields.io/badge/Tabletop-Sample-blue)](https://developers.arcgis.com/net/latest/ios/sample-code/display-scenes-in-tabletop-ar/) [![Flyover sample](https://img.shields.io/badge/Flyover-Sample-blue)](https://developers.arcgis.com/net/latest/ios/sample-code/explore-scenes-in-flyover-ar/)

[Augmented reality](https://developers.arcgis.com/net/latest/forms/guide/display-scenes-in-augmented-reality.htm) experiences are designed to "augment" the physical world with virtual content that respects real world scale, position, and orientation of a device. In the case of Runtime, `ARSceneView` extends `SceneView` to display GIS data on top of a camera feed showing the physical world.

## AR patterns

The Augmented Reality (AR) toolkit component allows quick and easy integration of AR into your application for a wide variety of scenarios. The toolkit recognizes the following common patterns for AR: 

* **Flyover**: Flyover AR allows you to explore a scene using your device as a window into the virtual world. A typical flyover AR scenario will start with the scene’s virtual camera positioned over an area of interest. You can walk around and reorient the device to focus on specific content in the scene. You can this demonstrated in the [Explore scenes in flyover AR](https://developers.arcgis.com/net/latest/ios/sample-code/explore-scenes-in-flyover-ar/) sample.
* **Tabletop**: Scene content is anchored to a physical surface, as if it were a 3D-printed model. You can see this demonstrated in the [Display scenes in tabletop AR](https://developers.arcgis.com/net/latest/ios/sample-code/display-scenes-in-tabletop-ar/) sample.
* **World-scale**: Scene content is rendered exactly where it would be in the physical world. A camera feed is shown and GIS content is rendered on top of that feed. This is used in scenarios ranging from viewing hidden infrastructure to displaying waypoints for navigation. You can see this demonstrated in the [Navigate in AR](https://developers.arcgis.com/net/latest/ios/sample-code/navigate-in-ar/) sample.

The AR toolkit component is comprised of one class: `ARSceneView`. This is a subclass of `SceneView` that contains the functionality needed to display an AR experience in your application. It uses the native platform's augmented reality framework to display the live camera feed and handle real world tracking and synchronization with the Runtime SDK's `SceneView`. `ARSceneView` is responsible for starting and managing an ARCore/ARKit session. `ARSceneView` uses a user-provided `LocationDataSource` for getting an initial GPS location and when continuous GPS tracking is required.

## Features of the AR component

* Allows display of the live camera feed
* Supports both ARKit on iOS and ARCore on Android
* Tracks user location and device orientation through ARKit/ARCore
* Provides access to an `SceneView` to display your GIS 3D data over the live camera feed
* Provides `ARScreenToLocation` method to convert a screen point to a real-world coordinate, taking into consideration real-world physical surfaces

## Visual Studio Templates

There is a significant amount of configuration needed to get iOS and Android projects ready for AR. The full details are covered below and in our guide under *Configure privacy and permissions* in [Display scenes in augmented reality](https://developers.arcgis.com/net/latest/forms/guide/display-scenes-in-augmented-reality.hm)

The ArcGIS Runtime templates for AR provide project templates that are usable out-of-the-box. You can download the templates from [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=Esri.EsriArcGISRuntimeARTemplates).

> **NOTE**: These templates are completely optional for using AR. See steps below if you're working with an existing project or prefer manual configuration.

## Configure your project for AR

The first step is to install the toolkit package from [Nuget](https://www.nuget.org/packages/Esri.ArcGISRuntime.ARToolkit/). Next, configure your app projects:

### Configuration for iOS

1. Ensure that the camera and location strings are set in **Info.plist**. You must provide values for *Privacy - Camera Usage Description* and *Privacy - Location When In Use Usage Description*.
2. If you want your app to only be available on devices supporting ARKit, add `arkit` to the required device capabilities section of **Info.plist**:

```xml
<key>UIRequiredDeviceCapabilities</key>
<array>
    <string>arkit</string>
</array>
```

### Configuration for Android

> **NOTE**: The device must support ARCore for `ARSceneView` to work on Android. Google maintains a [list of supported devices](https://developers.google.com/ar/discover/supported-devices). ARCore is a separate installable component delivered via Google Play. 

1. Ensure that `android.permission.CAMERA` and `android.permission.ACCESS_FINE_LOCATION` are specified in **AndroidManifest.xml**.
2. Specify that ARCore is a required or optional component of the app in **AndroidManifest.xml**. This will cause Google Play to download ARCore alongside your app automatically.
    ```xml
    <!-- Indicates that app requires ARCore ("AR Required"). Causes Google
            Play Store to download and install ARCore along with the app.
            For an "AR Optional" app, specify "optional" instead of "required".
        -->
    <application ...>
        <meta-data android:name="com.google.ar.core" android:value="required" />
    </application>
    ```
3. If you want your app to only be available on devices supporting ARCore, specify that the feature is required in **AndroidManifest.xml**: 
    ```xml
    <!-- Indicates that app requires ARCore ("AR Required"). Ensures app is only
             visible in the Google Play Store on devices that support ARCore.
             For "AR Optional" apps remove this line.
        -->
    <application ...>
        <uses-feature android:name="android.hardware.camera.ar" android:required="true" />
    </application>
    ```

## Usage

See [Display scenes in augmented reality](https://developers.arcgis.com/net/latest/forms/guide/display-scenes-in-augmented-reality.htm) for a full walkthrough for using ArcGIS for AR, with step-by-step tutorials for world-scale, flyover, and tabletop AR.

The first step to using AR is to enable and disable AR tracking with the appropriate lifecycle methods. 

* Forms:
    ```csharp
    public partial class BasicAR : ContentPage
    {
        protected override void OnAppearing()
        {
            base.OnAppearing();
            await ARView.StartTrackingAsync(Esri.ArcGISRuntime.ARToolkit.ARLocationTrackingMode.Ignore);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ARView.StopTrackingAsync();
        }
    }
    ```
* iOS:
    ```csharp
    public class BasicARExample : UIViewController
    {
        private ARSceneView _arSceneView;

        public override void LoadView()
        {
            // Create the views.
            View = new UIView();

            _arSceneView = new ARSceneView();
            _arSceneView.TranslatesAutoresizingMaskIntoConstraints = false;

            // Add the views.
            View.AddSubviews(_arSceneView);

            // Lay out the views.
            NSLayoutConstraint.ActivateConstraints(new[]{
                _arSceneView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _arSceneView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                _arSceneView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _arSceneView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor)
            });
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            Initialize();
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            await _arSceneView.StartTrackingAsync();
        }

        public override async void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            if (_arSceneView != null)
            {
                await _arView.StopTrackingAsync()
            }
        }
    }
    ```
* Android
    ```csharp
    public class BasicARExample : AppCompatActivity
    {
        // Hold references to the UI controls.
        private ARSceneView _arSceneView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            CreateLayout();
        }

        private void CreateLayout()
        {
            SetContentView(MyArApp.Resource.Layout.BasicARExample);
            _arSceneView = FindViewById<ARSceneView>(ArcGISRuntime.Resource.Id.arSceneView);
        }

        protected override async void OnPause()
        {
            base.OnPause();
            await _arSceneView.StopTrackingAsync();

        }

        protected override async void OnResume()
        {
            base.OnResume();
            await _arSceneView.StartTrackingAsync(ARLocationTrackingMode.Ignore);
        }
    }
    ```

## Next steps

* See [Display scenes in augmented reality](https://developers.arcgis.com/net/latest/android/guide/display-scenes-in-augmented-reality.htm) for a comprehensive guide to working with augmented reality and ArcGIS Runtime
* See the samples for working examples you can try today:
    * [Collect data in AR](https://developers.arcgis.com/net/latest/ios/sample-code/collect-data-in-ar/)
    * [Display scenes in tabletop AR](https://developers.arcgis.com/net/latest/ios/sample-code/display-scenes-in-tabletop-ar/)
    * [Explore scenes in flyover AR](https://developers.arcgis.com/net/latest/ios/sample-code/explore-scenes-in-flyover-ar/)
    * [Navigate in AR](https://developers.arcgis.com/net/latest/ios/sample-code/navigate-in-ar/)
    * [View hidden infrastructure in AR](https://developers.arcgis.com/net/latest/ios/sample-code/view-hidden-infrastructure-in-ar/)