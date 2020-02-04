---
uid:B1CCBC8A-91A2-485A-97E0-CF2B9F692B26
---

# BookmarksView

## Features

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

To show modally in a way that works well on iPad and iPhone:

```cs
public partial class BookmarksModalSampleViewController : UIViewController
{
    // Hold references to UI controls.
    private MapView _myMapView;
    private BookmarksView _bookmarksView;
    private UIBarButtonItem _showBookmarksButton;

    public BookmarksModalSampleViewController()
    {
        Title = "Show bookmarks modally";
    }

    private void Initialize()
    {
        // Create and show the map.
        _myMapView.Map = new Map(new Uri("https://arcgisruntime.maps.arcgis.com/home/item.html?id=16f1b8ba37b44dc3884afc8d5f454dd2"));
    }

    public override void LoadView()
    {
        // Create the views.
        View = new UIView() { BackgroundColor = UIColor.White };

        _myMapView = new MapView();
        _myMapView.TranslatesAutoresizingMaskIntoConstraints = false;

        _bookmarksView = new BookmarksView();
        _bookmarksView.GeoView = _myMapView;

        _showBookmarksButton = new UIBarButtonItem(UIBarButtonSystemItem.Bookmarks);

        // Note: this won't work if there's no navigation controller.
        NavigationItem.RightBarButtonItem = _showBookmarksButton;

        // Add the views.
        View.AddSubviews(_myMapView);

        // Lay out the views.
        NSLayoutConstraint.ActivateConstraints(new[]{
            _myMapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
            _myMapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
            _myMapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
            _myMapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor)
        });
    }

    private void _bookmarksView_BookmarkSelected(object sender, Bookmark e)
    {
        DismissModalViewController(true);
    }

    private void ShowBookmarks_Clicked(object sender, EventArgs e)
    {
        // Note: BookmarksView is a UIViewController.
        // This shows bookmarks modally on iPhone, in a popover on iPad
        _bookmarksView.ModalPresentationStyle = UIModalPresentationStyle.Popover;
        if (_bookmarksView.PopoverPresentationController is UIPopoverPresentationController popoverPresentationController)
        {
            popoverPresentationController.Delegate = new ppDelegate();
            popoverPresentationController.BarButtonItem = _showBookmarksButton;
        }
        // Attempting to show a VC while it is already being presented is an issue (can happen with rapid tapping)
        try
        {
            PresentModalViewController(_bookmarksView, true);
        }
        catch (MonoTouchException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            // Ignore
        }
    }

    /// <summary>
    /// Helper class, used to ensure modal appears properly for each presentation style
    /// </summary>
    private class ppDelegate : UIPopoverPresentationControllerDelegate
    {
        public override UIViewController GetViewControllerForAdaptivePresentation(UIPresentationController controller, UIModalPresentationStyle style)
        {
            return new UINavigationController(controller.PresentedViewController);
        }
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        Initialize();
    }

    public override void ViewWillAppear(bool animated)
    {
        base.ViewWillAppear(animated);

        if (_showBookmarksButton != null)
        {
            _showBookmarksButton.Clicked -= ShowBookmarks_Clicked;
            _showBookmarksButton.Clicked += ShowBookmarks_Clicked;
        }

        if (_bookmarksView != null)
        {
            // Listen for bookmark selections so that the view can be dismissed.
            _bookmarksView.BookmarkSelected -= _bookmarksView_BookmarkSelected;
            _bookmarksView.BookmarkSelected += _bookmarksView_BookmarkSelected;
        }
    }

    public override void ViewDidDisappear(bool animated)
    {
        base.ViewDidDisappear(animated);

        // unsub from events to prevent leaks
        if (_showBookmarksButton != null)
        {
            _showBookmarksButton.Clicked -= ShowBookmarks_Clicked;
        }

        if (_bookmarksView != null)
        {
            _bookmarksView.BookmarkSelected -= _bookmarksView_BookmarkSelected;
        }
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
