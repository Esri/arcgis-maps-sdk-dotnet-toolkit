using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.SearchView
{
    [SampleInfo(ApiKeyRequired = true)]
    public partial class SearchViewCustomizationSample : UserControl
    {
        public SearchViewCustomizationSample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySearchView.GeoView = MyMapView;
        }

        private void GeoViewConnection_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (EnableGeoViewBindingCheck.IsChecked ?? false)
            {
                MySearchView.GeoView = MyMapView;
            }
            else
            {
                MySearchView.GeoView = null;
            }
        }

        private async void AddDefaultLocator_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var source = await LocatorSearchSource.CreateDefaultSourceAsync();
                source.DisplayName = GeocoderNameTextBox.Text;
                MySearchView.SearchViewModel.Sources.Add(source);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message);
            }
        }

        private async void AddSMPLocator_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: You can download a sample of StreetMap Premium for testing purposes by visiting the downloads section of the ArcGIS Developer dashboard.
            string path = LocatorPathText.Text;
            if (!File.Exists(path))
            {
                MessageBox.Show("Error", "Input Path Does Not Exist");
                return;
            }

            try
            {
                MobileMapPackage mmpk = await MobileMapPackage.OpenAsync(path);
                if (mmpk.LocatorTask is LocatorTask packagedLocator)
                {
                    MySearchView.SearchViewModel.Sources.Add(new LocatorSearchSource(packagedLocator));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message);
            }
        }

        private void RemoveLocator_Click(object sender, RoutedEventArgs e)
        {
            if (MySearchView.SearchViewModel?.Sources?.Count > 0)
            {
                MySearchView.SearchViewModel.Sources.RemoveAt(MySearchView.SearchViewModel.Sources.Count - 1);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SearchModeCombo.SelectedIndex)
            {
                case 0:
                    MySearchView.SearchViewModel.SearchMode = SearchResultMode.Automatic;
                    break;
                case 1:
                    MySearchView.SearchViewModel.SearchMode = SearchResultMode.Single;
                    break;
                case 2:
                    MySearchView.SearchViewModel.SearchMode = SearchResultMode.Multiple;
                    break;
            }
        }

        private void AddTestLocator_Click(object sender, RoutedEventArgs e)
        {
            MySearchView.SearchViewModel.Sources.Add(new TestSearchSource());
        }

        private class TestSearchSource : ISearchSource
        {
            public string DisplayName { get => "Event tester"; set => throw new NotImplementedException(); }
            public string Placeholder { get => "Test placeholder"; set => throw new NotImplementedException(); }
            public CalloutDefinition DefaultCalloutDefinition { get => null; set => throw new NotImplementedException(); }
            public Symbol DefaultSymbol { get => null; set => throw new NotImplementedException(); }
            public double DefaultZoomScale { get => 1000; set => throw new NotImplementedException(); }
            public int MaximumResults { get => 3; set => throw new NotImplementedException(); }
            public int MaximumSuggestions { get => 3; set => throw new NotImplementedException(); }
            public Geometry.Geometry SearchArea { get => null; set { } }
            public MapPoint PreferredSearchLocation { get => null; set { } }

            public void NotifyDeselected(SearchResult result)
            {
                MessageBox.Show($"Deselected {result?.DisplayTitle ?? "all results"}");
            }

            public void NotifySelected(SearchResult result)
            {
                MessageBox.Show($"Selected {result.DisplayTitle}");
            }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            public async Task<IList<SearchResult>> RepeatSearchAsync(string queryString, Envelope queryExtent, CancellationToken cancellationToken)
            {
                var list = new [] {"one", "two", "three", "four"};
                return list.Select(m => new SearchResult($"repeat {m}", "repeat subtitle", this, null, null)).ToList();
            }

            public async Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken cancellationToken)
            {
                var list = new [] {"one", "two", "three", "four"};
                return list.Select(m => new SearchResult($"explicit search {m}", "repeat subtitle", this, null, null)).ToList();
            }

            public async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken cancellationToken)
            {
                if (suggestion.IsCollection)
                {
                    var list = new [] {"one", "two", "three", "four"};
                    return list.Select(m => new SearchResult($"search from suggestion - res: {m}", suggestion.DisplayTitle, this, null, null)).ToList();
                }
                else
                {
                    return new List<SearchResult>() { new SearchResult($"result from suggestion {suggestion.DisplayTitle}", "from suggestion", this, null, null)};
                }
            }

            public async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken cancellationToken)
            {
                var list = new [] {"one", "two", "three", "four"};
                return list.Select(m => new SearchSuggestion($"suggestion {m}", this) { IsCollection = m.Contains("w")}).ToList();
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }
    }
}
