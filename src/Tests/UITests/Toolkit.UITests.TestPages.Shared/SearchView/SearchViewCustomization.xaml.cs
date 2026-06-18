#if MAUI_APP
using ClickEventArgs = System.EventArgs;
using Esri.ArcGISRuntime.Toolkit.Maui;
#elif WINUI_APP
using System;
using ClickEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Microsoft.UI.Xaml;
#elif WPF_APP
using System.Globalization;
using ClickEventArgs = System.Windows.RoutedEventArgs;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Microsoft.UI.Xaml;
#endif
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


namespace Toolkit.UITests.App.TestPages;

public partial class SearchViewCustomization : TestPage
{
    public SearchViewCustomization()
    {
        InitializeComponent();

        MyMapView.Map = new Esri.ArcGISRuntime.Mapping.Map(BasemapStyle.ArcGISImagery);
        MySearchView.GeoView = MyMapView;
    }

    private void AddEventTestSourceButton_Click(object sender, ClickEventArgs e)
    {
        MySearchView.SearchViewModel?.Sources.Add(new TestSearchSource(text => VerifyEventTextBlock.Text = text));
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
#if WINUI_APP || WPF_APP
        MySearchView.SearchTooltipText = "Custom Search";
        MySearchView.ClearSearchTooltipText = "Custom Clear Search";
        MySearchView.AllSourceSelectText = "Custom All Sources";
#elif MAUI_APP
        MySearchView.AllSourcesSelectText = "Custom All Sources";
#endif
        MySearchView.SearchViewModel?.DefaultPlaceholder = "Custom Find a place or address";
        MySearchView.RepeatSearchButtonText = "Custom Repeat Search Here";
        MySearchView.NoResultMessage = "Custom No Results";
    }

    private void UpdateViewpoint_Click(double scale, double longitude, double latitude)
    {
        var center = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
        MyMapView.SetViewpoint(new Viewpoint(center, scale));
    }

#if WPF_APP || WINUI_APP
    private void GeoViewConnection_Checked(object sender, RoutedEventArgs e)
    {
        if (EnableGeoViewBindingCheck.IsChecked ?? false)
#elif MAUI_APP

    private void GeoViewConnection_Checked(object sender, CheckedChangedEventArgs e)
    {
        // Guard against exception on iOS device
        if (MySearchView == null || MyMapView == null)
        {
            return;
        }

        if (e.Value)
#endif
        {
            MySearchView.GeoView = MyMapView;
        }
        else
        {
            MySearchView.GeoView = null;
        }
    }

    private class TestSearchSource : ISearchSource
    {
        private readonly System.Action<string> _updateEventText;

        public TestSearchSource(System.Action<string> updateEventText)
        {
            _updateEventText = updateEventText;
        }
        public string DisplayName { get => "Event tester"; set => throw new NotImplementedException(); }
        public string Placeholder { get => "Test placeholder"; set => throw new NotImplementedException(); }
        public CalloutDefinition DefaultCalloutDefinition { get => null; set => throw new NotImplementedException(); }
        public double DefaultZoomScale { get => 1000; set => throw new NotImplementedException(); }
        public int MaximumResults { get => 3; set => throw new NotImplementedException(); }
        public int MaximumSuggestions { get => 3; set => throw new NotImplementedException(); }
        public Geometry? SearchArea { get => null; set { } }
        public MapPoint? PreferredSearchLocation { get => null; set { } }

        Esri.ArcGISRuntime.Symbology.Symbol? ISearchSource.DefaultSymbol { get => null; set => throw new NotImplementedException(); }

        public void NotifyDeselected(SearchResult? result)
        {
            _updateEventText("Deselected event fired");
        }

        public void NotifySelected(SearchResult? result)
        {
            _updateEventText("Selected event fired");
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<IList<SearchResult>> RepeatSearchAsync(string queryString, Envelope queryExtent, CancellationToken cancellationToken)
        {
            var list = new[] { "one", "two", "three", "four" };
            return list.Select(m => new SearchResult($"repeat {m}", "repeat subtitle", this, null, null)).ToList();
        }

        public async Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken cancellationToken)
        {
            var list = new[] { "one", "two", "three", "four" };
            return list.Select(m => new SearchResult($"explicit search {m}", "repeat subtitle", this, null, null)).ToList();
        }

        public async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken cancellationToken)
        {
            if (suggestion.IsCollection)
            {
                var list = new[] { "one", "two", "three", "four" };
                return list.Select(m => new SearchResult($"search from suggestion - res: {m}", suggestion.DisplayTitle, this, null, null)).ToList();
            }
            else
            {
                return new List<SearchResult>() { new SearchResult($"result from suggestion {suggestion.DisplayTitle}", "from suggestion", this, null, null) };
            }
        }

        public async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken cancellationToken)
        {
            var list = new[] { "one", "two", "three", "four" };
            return list.Select(m => new SearchSuggestion($"suggestion {m}", this) { IsCollection = m.Contains("w") }).ToList();
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}