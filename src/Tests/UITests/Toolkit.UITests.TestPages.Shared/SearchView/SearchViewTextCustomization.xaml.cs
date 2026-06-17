#if MAUI_APP
using ClickEventArgs = System.EventArgs;
#elif WINUI_APP
using System;
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
#elif WPF_APP
using System.Globalization;
using ClickEventArgs = System.Windows.RoutedEventArgs;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Toolkit.UITests.App.TestPages;

#if WPF_APP
public partial class SearchViewTextCustomization : TestPage
{
    public SearchViewTextCustomization()
    {
        InitializeComponent();

        var map = new Map(BasemapStyle.ArcGISImagery);
        MyMapView.Map = map;
        AddDefaultLocatorWithName();
    }

    private void UpdateViewpointExtentToOntario_Click(object sender, ClickEventArgs e)
    {
        UpdateViewpoint_Click(60000, -117.602000, 34.055845);
    }

    private void UpdateViewpointExtentToColorado_Click(object sender, ClickEventArgs e)
    {
        UpdateViewpoint_Click(3000000, -105.143243, 38.888975);
    }

    private void UpdateToCustomValuesButton_Click(object sender, ClickEventArgs e)
    {
        SearchTooltipText.Text = "Custom Search";
        ClearSearchTooltipText.Text = "Custom Clear Search";
        DefaultPlaceholderText.Text = "Custom Find a place or address";
        RepeatSearchButtonText.Text = "Custom Repeat Search Here";
        AllSourceButtonText.Text = "Custom All Sources";
        NoResultMessageText.Text = "Custom No Results";
    }

    private void UpdateViewpoint_Click(double scale, double longitude, double latitude)
    {
        var center = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
        MyMapView.SetViewpoint(new Viewpoint(center, scale));
    }
    private async void AddDefaultLocatorWithName()
    {
        try
        {
            var source = await LocatorSearchSource.CreateDefaultSourceAsync();
            source.DisplayName = "source with name";
            MySearchView.SearchViewModel?.Sources.Add(source);       
        }
        catch (Exception)
        {
        }
    }
}
#endif