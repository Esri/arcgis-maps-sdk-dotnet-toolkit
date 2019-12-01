# Docs: Bookmarks

Features:

* Associate the bookmarks control with a `MapView` or `SceneView` (`GeoView` property), through binding on supported platforms (WPF, UWP, Forms) or plain properties otherwise.
* Display a list of bookmarks, defined by `BookmarkList` property or the `Map` or `Scene` from the associated `GeoView`.
* Navigates the associated `GeoView` to the selected bookmark.
* Specify whether to display the list from `BookmarkList` or the `Map`/`Scene` `Bookmarks` property using the `PrefersBookmarksList` property.
* Customize the display of the list with the `ItemTemplate` property on UWP and WPF.
* Works well with `INotifyCollectionChanged` and handles changes to the `Map`/`Scene` properties.
* On iOS, an additional `BookmarksVC` view controller is provided to facilitate modal presentation.

## Platform-specific usage

> **NOTE**: See the samples projects to see various usages of this component in action. Samples are available for all supported platforms.

### iOS

There are two ways to use the bookmarks view on iOS:

* As a view; you specify the layout of the view within your view hierarchy
* As a view controller; you create `BookmarksVC` with a configured instance of `Bookmarks` and present it modally.

As a view:

```csharp
public partial class BookmarksMapViewController : UIViewController
    {
        private Bookmarks bookmarks;
        private MapView mapView;

        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            mapView = new MapView()
            {
                Map = new Map(new Uri(_mapUrl)),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

             this.View.AddSubview(mapView);

            // Create the bookmarks view, referencing the mapview.
            bookmarks = new Bookmarks()
            {
                GeoView = mapView,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            this.View.AddSubview(bookmarks);

            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            bookmarks.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            bookmarks.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            bookmarks.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            bookmarks.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
        }
    }
```

As a modally-presented view controller:

```csharp
public partial class BookmarksMapViewControllerAlt : UIViewController
    {
        private Bookmarks bookmarks;
        private BookmarksVC _bookmarksVC;

        private MapView mapView;
        private UIBarButtonItem _showBookmarksButton;

        private const string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Create the base view.
            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            // Configure and show the mapview.
            mapView = new MapView()
            {
                Map = new Map(new Uri(_mapUrl)),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

             this.View.AddSubview(mapView);

            // Create the bookmarks view, referencing the existing map view.
            bookmarks = new Bookmarks()
            {
                GeoView = mapView,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            // Only the map view is shown here; presentation of the bookmarks control is handled by _bookmarksVC.
            mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;

            // Button added in the navigation bar to show the bookmarks control.
            _showBookmarksButton = new UIBarButtonItem("Bookmarks", UIBarButtonItemStyle.Plain, ShowBookmarksClicked);
            NavigationItem.SetRightBarButtonItem(_showBookmarksButton, false);
        }

        private void ShowBookmarksClicked(object sender, EventArgs e)
        {
            // Lazily create the view controller only when its needed.
            if (_bookmarksVC == null)
            {
                // The bookmarks view controller must be created with an existing bookmarks view.
                _bookmarksVC = new BookmarksVC(bookmarks);
            }
            // Show the bookmarks view controller.
            PresentModalViewController(new UINavigationController(_bookmarksVC), true);
        }
    }
```

### Android

The bookmarks control can be used through code or XML layout.

XML layout:

```xml
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:orientation="vertical"
    android:layout_width="match_parent"
    android:layout_height="match_parent">
    <Esri.ArcGISRuntime.UI.Controls.MapView
        android:layout_width="match_parent"
        android:layout_height="match_parent"
        android:layout_weight="1"
        android:id="@+id/mapView" />
    <Esri.ArcGISRuntime.Toolkit.UI.Controls.Bookmarks
        android:layout_width="match_parent"
        android:layout_height="match_parent"
        android:layout_weight="1"
        android:id="@+id/bookmarksView" />
</LinearLayout>
```

C# code:

```csharp
[Activity(Label = "Bookmarks", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
public class BookmarksSampleActivity : Activity
{
    private MapView mapView;
    private Bookmarks bookmarksView;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.BookmarksSample);
        mapView = FindViewById<MapView>(Resource.Id.mapView);
        mapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee"));
        bookmarksView = FindViewById<Bookmarks>(Resource.Id.bookmarksView);
        bookmarksView.GeoView = mapView;
    }
}
```

### Xamarin Forms

On Xamarin Forms, there are two ways to configure the `Bookmarks` control:

* Binding in XAML
* Setting properties in code

The following properties support binding:

* `PrefersBookmarksList`
* `BookmarkList`
* `GeoView`

Binding example:

```xml
<ContentPage xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:d="http://xamarin.com/schemas/2014/forms/design"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:toolkit="clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;assembly=Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"
             xmlns:esri="clr-namespace:Esri.ArcGISRuntime.Xamarin.Forms;assembly=Esri.ArcGISRuntime.Xamarin.Forms"
             x:Class="Toolkit.Samples.Forms.Samples.BookmarksSample">
    <ContentPage.Content>
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <esri:MapView x:Name="MyMapView" Grid.Column="0" />
            <toolkit:Bookmarks Grid.Column="1"
                               GeoView="{x:Reference MyMapView}" />
        </Grid>
    </ContentPage.Content>
</ContentPage>
```

The above will automatically support showing the list of bookmarks and navigating to the selected bookmark.

### WPF & UWP

On WPF and UWP, the bookmarks control supports configuration through binding or by setting properties in code. Additionally, `Bookmarks` supports customization of bookmark display via the `ItemTemplate` property.

Binding example (WPF):

```xml
<UserControl x:Class="Esri.ArcGISRuntime.Toolkit.Samples.Bookmarks.MapBookmarksSample"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <esri:MapView x:Name="MyMapView" Grid.Column="0" />
        <esri:Bookmarks Grid.Column="1"
                        GeoView="{Binding ElementName=MyMapView}" />
    </Grid>
</UserControl>
```

Binding example (UWP):

```xml
<Page
    x:Class="Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Bookmarks.BookmarksMapSample"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mapping="using:Esri.ArcGISRuntime.Mapping"
    xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
    xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <esri:MapView x:Name="MyMapView" Grid.Column="0" />
        <toolkit:Bookmarks GeoView="{Binding ElementName=MyMapView}" Grid.Column="1" />
    </Grid>
</Page>
```

List customization example (WPF):

```xml
<UserControl x:Class="Esri.ArcGISRuntime.Toolkit.Samples.Bookmarks.MapBookmarksSample"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:esri="http://schemas.esri.com/arcgis/runtime/2013">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <esri:MapView x:Name="MyMapView" Grid.Column="0" />
        <esri:Bookmarks Grid.Column="1"
                        GeoView="{Binding ElementName=MyMapView}">
            <esri:Bookmarks.ItemTemplate>
                <DataTemplate>
                    <Grid HorizontalAlignment="Right">
                        <TextBlock Text="{Binding Name}" Foreground="Red" />
                    </Grid>
                </DataTemplate>
            </esri:Bookmarks.ItemTemplate>
        </esri:Bookmarks>
    </Grid>
</UserControl>
```

List customization example (UWP):

```xml
<Page
    x:Class="Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.Bookmarks.BookmarksMapSample"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mapping="using:Esri.ArcGISRuntime.Mapping"
    xmlns:esri="using:Esri.ArcGISRuntime.UI.Controls"
    xmlns:toolkit="using:Esri.ArcGISRuntime.Toolkit.UI.Controls"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <esri:MapView x:Name="MyMapView" Grid.Column="0" />
        <toolkit:Bookmarks GeoView="{Binding ElementName=MyMapView}" Grid.Column="1">
            <toolkit:Bookmarks.ItemTemplate>
                <DataTemplate x:DataType="mapping:Bookmark">
                    <TextBlock Text="{Binding Name}" Foreground="Red" />
                </DataTemplate>
            </toolkit:Bookmarks.ItemTemplate>
        </toolkit:Bookmarks>
    </Grid>
</Page>
```