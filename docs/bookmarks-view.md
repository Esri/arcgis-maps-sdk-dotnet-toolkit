# Docs: BookmarksView

Features:

* Associate the bookmarks control with a `MapView` or `SceneView` (`GeoView` property), through binding on supported platforms (WPF, UWP, Forms) or plain properties otherwise.
* Display a list of bookmarks, defined by the `Map` or `Scene` from the associated `GeoView` or the `BookmarksOverride` if set.
* Navigates the associated `GeoView` to the selected bookmark.
* Customize the display of the list with the `ItemTemplate` property on UWP and WPF.
* Supports observable collections for `BookmarksOverride` and handles changes to the `Map`/`Scene` properties.

## Platform-specific usage

> **NOTE**: See the samples projects to see various usages of this component in action. Samples are available for all supported platforms.

### iOS

On iOS, `BookmarksView` is a UIViewController, which can be shown directly, or added as a child view controller to show its view.

As a view:

```csharp
public partial class BookmarksMapEventTestController : UIViewController
    {
        private BookmarksView _bookmarksView;
        private MapView _mapView;

        private readonly string _mapUrl = "https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee";

        private ObservableCollection<Bookmark> _bookmarksObservable = new ObservableCollection<Bookmark>();

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            configureManualList();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _mapView = new MapView()
            {
                Map = new Map(new Uri(_mapUrl)),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            View.AddSubview(_mapView);

            _bookmarksView = new BookmarksView()
            {
                GeoView = _mapView
            };

            AddChildViewController(_bookmarksView);
            _bookmarksView.View.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(_bookmarksView.View);

            _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            _mapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            _mapView.BottomAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;

            _bookmarksView.View.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            _bookmarksView.View.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            _bookmarksView.View.TopAnchor.ConstraintEqualTo(View.CenterYAnchor).Active = true;
            _bookmarksView.View.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
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
    <Esri.ArcGISRuntime.Toolkit.UI.Controls.BookmarksView
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
    private MapView _mapView;
    private BookmarksView _bookmarksView;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.BookmarksSample);
        mapView = FindViewById<MapView>(Resource.Id.mapView);
        mapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/webmap/viewer.html?webmap=1c45a922e9e7465295323f4d2e7e42ee"));
        _bookmarksView = FindViewById<Bookmarks>(Resource.Id.bookmarksView);
        _bookmarksView.GeoView = mapView;
    }
}
```

### Xamarin Forms

On Xamarin Forms, there are two ways to configure the `BookmarksView` control:

* Binding in XAML
* Setting properties in code

The following properties support binding:

* `BookmarksOverride`
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
            <toolkit:BookmarksView Grid.Column="1"
                               GeoView="{x:Reference MyMapView}" />
        </Grid>
    </ContentPage.Content>
</ContentPage>
```

The above will automatically support showing the list of bookmarks and navigating to the selected bookmark.

### WPF & UWP

On WPF and UWP, the bookmarks control supports configuration through binding or by setting properties in code. Additionally, `BookmarksView` supports customization of bookmark display via the `ItemTemplate` property.

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
        <esri:BookmarksView Grid.Column="1"
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
        <toolkit:BookmarksView GeoView="{Binding ElementName=MyMapView}" Grid.Column="1" />
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
            <esri:BookmarksView.ItemTemplate>
                <DataTemplate>
                    <Grid HorizontalAlignment="Right">
                        <TextBlock Text="{Binding Name}" Foreground="Red" />
                    </Grid>
                </DataTemplate>
            </esri:BookmarksView.ItemTemplate>
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
            <toolkit:BookmarksView.ItemTemplate>
                <DataTemplate x:DataType="mapping:Bookmark">
                    <TextBlock Text="{Binding Name}" Foreground="Red" />
                </DataTemplate>
            </toolkit:BookmarksView.ItemTemplate>
        </toolkit:Bookmarks>
    </Grid>
</Page>
```
