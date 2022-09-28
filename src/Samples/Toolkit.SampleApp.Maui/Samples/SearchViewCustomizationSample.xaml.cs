using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Maui;
using Esri.ArcGISRuntime.UI;

namespace Toolkit.SampleApp.Maui.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [SampleInfoAttribute(Category = "SearchView", Description = "Exercises bindings and advanced customization options.")]
    public partial class SearchViewCustomizationSample : ContentPage
    {
        public SearchViewCustomizationSample()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
            MySearchView.GeoView = MyMapView;
            SearchModePicker.Items.Add("Automatic");
            SearchModePicker.Items.Add("Single");
            SearchModePicker.Items.Add("Multiple");
            SearchModePicker.SelectedIndex = 0;
        }

        private void GeoViewConnection_Checked(object? sender, CheckedChangedEventArgs e)
        {
            // Guard against exception on iOS device
            if (MySearchView == null || MyMapView == null)
            {
                return;
            }

            if (e.Value)
            {
                MySearchView.GeoView = MyMapView;
            }
            else
            {
                MySearchView.GeoView = null;
            }
        }

        private async void AddDefaultLocator_Click(object? sender, EventArgs e)
        {
            try
            {
                var source = await LocatorSearchSource.CreateDefaultSourceAsync();
                source.DisplayName = GeocoderNameEntry.Text;
                MySearchView.SearchViewModel?.Sources.Add(source);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Ok");
            }
        }

        private void RemoveLocator_Click(object? sender, EventArgs e)
        {
            if (MySearchView.SearchViewModel?.Sources?.Count > 0)
            {
                MySearchView.SearchViewModel.Sources.RemoveAt(MySearchView.SearchViewModel.Sources.Count - 1);
            }
        }

        private void SearchModePicker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Guard against exception on iOS device
            if (MySearchView?.SearchViewModel == null || MyMapView == null || SearchModePicker == null)
            {
                return;
            }

            MySearchView.SearchViewModel.SearchMode = SearchModePicker.SelectedIndex switch
            {
                0 => SearchResultMode.Automatic,
                1 => SearchResultMode.Single,
                _ => SearchResultMode.Multiple
            };
        }

        private void AddTestLocator_Click(object? sender, EventArgs e)
        {
            if (MySearchView?.SearchViewModel != null)
            {
                MySearchView.SearchViewModel.Sources.Add(new TestSearchSource());
            }
        }
#pragma warning disable CS8603, CS8602, CS8769, CS8767
        private class TestSearchSource : ISearchSource
        {
            public string DisplayName { get => "Event tester"; set => throw new NotImplementedException(); }
            public string Placeholder { get => "Test placeholder"; set => throw new NotImplementedException(); }
            public CalloutDefinition DefaultCalloutDefinition { get => null; set => throw new NotImplementedException(); }
            public double DefaultZoomScale { get => 1000; set => throw new NotImplementedException(); }
            public int MaximumResults { get => 3; set => throw new NotImplementedException(); }
            public int MaximumSuggestions { get => 3; set => throw new NotImplementedException(); }
            public Geometry SearchArea { get => null; set { } }
            public MapPoint PreferredSearchLocation { get => null; set { } }

            Esri.ArcGISRuntime.Symbology.Symbol ISearchSource.DefaultSymbol { get => null; set => throw new NotImplementedException(); }

            public void NotifyDeselected(SearchResult result)
            {
                App.Current.MainPage.DisplayAlert("Deselected", $"Deselected {result?.DisplayTitle ?? "all results"}", "Ok");
            }

            public void NotifySelected(SearchResult result)
            {
                App.Current.MainPage.DisplayAlert("Selected", $"Selected {result?.DisplayTitle}", "Ok");
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
#pragma warning restore CS1998// Async method lacks 'await' operators and will run synchronously
        }
#pragma warning restore CS8603, CS8602, CS8769, CS8767
    }
}