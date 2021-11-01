// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using XForms = Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// View for searching with locators and custom search sources.
    /// </summary>
    public partial class SearchView : TemplatedView, INotifyPropertyChanged
    {
        // Controls how long the control waits after typing stops before looking for suggestions.
        private const int TypingDelayMilliseconds = 75;
        private GeoModel? _lastUsedGeomodel;
        private readonly GraphicsOverlay _resultOverlay;

        // Flag indicates whether control is waiting after user finished typing.
        private bool _waitFlag;

        // Flag indicating that query text is changing as a result of selecting a suggestion; view should not request suggestions in response to the user suggesting a selection.
        private bool _acceptingSuggestionFlag;

        private Entry? PART_Entry;
        private Button? PART_CancelButton;
        private Button? PART_SearchButton;
        private Button? PART_SourceSelectButton;
        private Label? PART_ResultLabel;
        private ListView? PART_SuggestionsView;
        private ListView? PART_ResultView;
        private ListView? PART_SourcesView;
        private Button? PART_RepeatButton;
        private static readonly DataTemplate DefaultResultTemplate;
        private static readonly DataTemplate DefaultSuggestionTemplate;
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly ByteArrayToImageSourceConverter ImageSourceConverter;

        static SearchView()
        {
            ImageSourceConverter = new ByteArrayToImageSourceConverter();
            DefaultSuggestionTemplate = new DataTemplate(() =>
            {
                var defaultCell = new ImageCell();
                defaultCell.SetBinding(ImageCell.TextProperty, nameof(SearchSuggestion.DisplayTitle));
                defaultCell.SetBinding(ImageCell.DetailProperty, nameof(SearchSuggestion.DisplaySubtitle));
                // TODO - add icons for collection and non-collection searches
                return defaultCell;
            });
            DefaultResultTemplate = new DataTemplate(() =>
            {
                var defaultCell = new ImageCell();
                defaultCell.SetBinding(ImageCell.TextProperty, nameof(SearchResult.DisplayTitle));
                defaultCell.SetBinding(ImageCell.DetailProperty, nameof(SearchResult.DisplaySubtitle));
                defaultCell.SetBinding(ImageCell.ImageSourceProperty, nameof(SearchResult.MarkerImageData), converter: ImageSourceConverter);
                return defaultCell;
            });

            string template =
$@"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" 
xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
<Grid BackgroundColor=""White"">
    <Grid.ColumnDefinitions>
    <ColumnDefinition Width=""Auto"" />
    <ColumnDefinition Width=""*"" />
    <ColumnDefinition Width=""120"" />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
    <RowDefinition Height=""Auto"" />
    <RowDefinition Height=""Auto"" />
    <RowDefinition Height=""Auto"" />
    </Grid.RowDefinitions>
    <Button x:Name=""{nameof(PART_SourceSelectButton)}"" Text=""Source"" Grid.Column=""0"" />
    <Entry x:Name=""{nameof(PART_Entry)}"" Grid.Column=""1"" Grid.Row=""0"" />
    <Button x:Name=""{nameof(PART_CancelButton)}"" Text=""X"" Grid.Column=""1"" HorizontalOptions=""End"" />
    <Button x:Name=""{nameof(PART_SearchButton)}"" Text=""Search"" Grid.Column=""2"" />
    <ListView x:Name=""{nameof(PART_SourcesView)}"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" />
    <ListView x:Name=""{nameof(PART_SuggestionsView)}"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" />
    <ListView x:Name=""{nameof(PART_ResultView)}"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" />
    <Label x:Name=""{nameof(PART_ResultLabel)}"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" />
    <Button x:Name=""{nameof(PART_RepeatButton)}"" Text=""Repeat Search Here"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""2"" />
</Grid>
</ControlTemplate>";
            DefaultControlTemplate = XForms.Extensions.LoadFromXaml(new ControlTemplate(), template);
        }

        public SearchView()
        {
            ResultTemplate = DefaultResultTemplate;
            SuggestionTemplate = DefaultSuggestionTemplate;
            ControlTemplate = DefaultControlTemplate;
            BindingContext = this;
            SearchViewModel = new SearchViewModel();
            NoResultMessage = "No Results";
            _resultOverlay = new GraphicsOverlay { Id = "SearchView_Result_Overlay" };
            ClearCommand = new DelegateCommand(HandleClearSearchCommand);
            SearchCommand = new DelegateCommand(HandleSearchCommand);
            RepeatSearchHereCommand = new DelegateCommand(HandleRepeatSearchHereCommand);
        }

        protected override void OnApplyTemplate()
        {
            if (PART_SourceSelectButton != null)
            {
                PART_SourceSelectButton.Clicked -= PART_SourceSelectButton_Clicked;
                PART_SourceSelectButton = null;
            }

            if (PART_Entry != null)
            {
                PART_Entry.TextChanged -= PART_Entry_TextChanged;
                PART_Entry = null;
            }

            if (PART_CancelButton != null)
            {
                PART_CancelButton.Clicked -= PART_CancelButton_Clicked;
                PART_CancelButton = null;
            }

            if (PART_SearchButton != null)
            {
                PART_SearchButton.Clicked -= PART_SearchButton_Clicked;
                PART_CancelButton = null;
            }

            if (PART_ResultLabel != null)
            {
                PART_ResultLabel = null;
            }

            if (PART_SourcesView != null)
            {
                PART_SourcesView.ItemTapped -= PART_SourcesView_ItemTapped;
                PART_SourcesView = null;
            }

            if (PART_SuggestionsView != null)
            {
                PART_SuggestionsView.ItemSelected -= PART_SuggestionsView_ItemSelected;
                PART_SuggestionsView.ItemsSource = null;
                PART_SuggestionsView = null;
            }

            if (PART_ResultView != null)
            {
                PART_ResultView.ItemSelected -= PART_ResultView_ItemSelected;
                PART_ResultView.ItemsSource = null;
                PART_ResultView = null;
            }

            if (PART_RepeatButton != null)
            {
                PART_RepeatButton.Clicked -= PART_RepeatButton_Clicked;
                PART_RepeatButton = null;
            }

            base.OnApplyTemplate();

            if (GetTemplateChild(nameof(PART_SourceSelectButton)) is Button newSourceButton)
            {
                PART_SourceSelectButton = newSourceButton;
                PART_SourceSelectButton.Clicked += PART_SourceSelectButton_Clicked;
                PART_SourceSelectButton.IsVisible = SearchViewModel?.Sources?.Count > 1;
            }

            if (GetTemplateChild(nameof(PART_Entry)) is Entry newEntry)
            {
                PART_Entry = newEntry;
                PART_Entry.Text = SearchViewModel?.CurrentQuery;
                PART_Entry.Placeholder = SearchViewModel?.ActivePlaceholder;
                PART_Entry.TextChanged += PART_Entry_TextChanged;
            }

            if (GetTemplateChild(nameof(PART_CancelButton)) is Button newCancel)
            {
                PART_CancelButton = newCancel;
                PART_CancelButton.IsVisible = !string.IsNullOrEmpty(PART_Entry?.Text);
                PART_CancelButton.Clicked += PART_CancelButton_Clicked;
            }

            if (GetTemplateChild(nameof(PART_SearchButton)) is Button newSearch)
            {
                PART_SearchButton = newSearch;
                PART_SearchButton.Clicked += PART_SearchButton_Clicked;
            }

            if (GetTemplateChild(nameof(PART_ResultLabel)) is Label newResultLabel)
            {
                PART_ResultLabel = newResultLabel;
                PART_ResultLabel.Text = NoResultMessage;
                PART_ResultLabel.IsVisible = SearchViewModel?.Results != null && SearchViewModel.Results.Any();
            }

            if (GetTemplateChild(nameof(PART_SourcesView)) is ListView newSourceSelectView)
            {
                PART_SourcesView = newSourceSelectView;
                PART_SourcesView.IsVisible = false;
                PART_SourcesView.ItemTapped += PART_SourcesView_ItemTapped;
            }

            if (GetTemplateChild(nameof(PART_ResultView)) is ListView newResultList)
            {
                PART_ResultView = newResultList;
                PART_ResultView.ItemTemplate = ResultTemplate;
                PART_ResultView.IsVisible = false;
                // TODO = consider using ItemTapped instead
                PART_ResultView.ItemSelected += PART_ResultView_ItemSelected;
            }

            if (GetTemplateChild(nameof(PART_SuggestionsView)) is ListView newSuggestionList)
            {
                PART_SuggestionsView = newSuggestionList;
                PART_SuggestionsView.ItemTemplate = SuggestionTemplate;
                PART_SuggestionsView.ItemSelected += PART_SuggestionsView_ItemSelected;
                PART_SuggestionsView.IsVisible = false;
            }

            if (GetTemplateChild(nameof(PART_RepeatButton)) is Button newRepeatButton)
            {
                PART_RepeatButton = newRepeatButton;
                PART_RepeatButton.IsVisible = SearchViewModel?.IsEligibleForRequery ?? false;
                PART_RepeatButton.Clicked += PART_RepeatButton_Clicked;
            }
        }

        private void PART_SourcesView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.ItemIndex == 0)
            {
                SearchViewModel.ActiveSource = null;
            }
            else
            {
                SearchViewModel.ActiveSource = SearchViewModel.Sources[e.ItemIndex - 1];
            }
            PART_SourcesView.IsVisible = false;
        }

        private void PART_RepeatButton_Clicked(object sender, EventArgs e)
        {
            RepeatSearchHereCommand?.Execute(null);
        }

        private void PART_SuggestionsView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItemIndex != -1)
            {
                SearchViewModel.AcceptSuggestion(SearchViewModel.Suggestions[e.SelectedItemIndex]);
                PART_SuggestionsView.SelectedItem = null;
            }
        }

        private void PART_ResultView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItemIndex != -1)
            {
                SearchViewModel.SelectedResult = SearchViewModel.Results[e.SelectedItemIndex];
                PART_ResultView.SelectedItem = null;
            }
        }

        private async void PART_SourceSelectButton_Clicked(object sender, EventArgs e)
        {
            if (PART_SourcesView.IsVisible)
            {
                PART_SourcesView.IsVisible = false;
                return;
            }
            var sources =  new[] { "All Source" }.Concat(SearchViewModel.Sources.Select(source => source.DisplayName)).ToList();

            PART_SourcesView.ItemsSource = sources;
            if (SearchViewModel.ActiveSource == null)
            {
                PART_SourcesView.SelectedItem = sources[0];
            }
            else
            {
                PART_SourcesView.SelectedItem = sources[SearchViewModel.Sources.IndexOf(SearchViewModel.ActiveSource) + 1];
            }
            PART_SourcesView.IsVisible = true;
        }

        private void PART_Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchViewModel.CurrentQuery = e.NewTextValue;
        }

        private void PART_CancelButton_Clicked(object sender, EventArgs e)
        {
            this.ClearCommand.Execute(null);
        }

        private void PART_SearchButton_Clicked(object sender, EventArgs e)
        {
            this.SearchCommand.Execute(null);
        }

        private async Task ConfigureForCurrentMap()
        {
            if (!EnableAutomaticConfiguration)
            {
                return;
            }

            try
            {
                if (GeoView is MapView mv && mv.Map is Map map)
                {
                    await SearchViewModel?.ConfigureFromMap(map, null);
                }
                else if (GeoView is SceneView sv && sv.Scene is Scene sp)
                {
                    await SearchViewModel?.ConfigureFromMap(sp, null);
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private void AddResultToGeoView(SearchResult result)
        {
            if (result?.GeoElement is Graphic graphic)
            {
                _resultOverlay.Graphics.Add(graphic);
            }
        }

        #region Binding support

        /// <summary>
        /// Gets or sets the selected suggestion, triggering a search.
        /// </summary>
        public SearchSuggestion? SelectedSuggestion
        {
            get => null;
            set
            {
                // ListView calls selecteditem binding with null when collection is cleared.
                if (value is SearchSuggestion userSelection)
                {
                    _acceptingSuggestionFlag = true;
                    _ = SearchViewModel?.AcceptSuggestion(userSelection)
                                       .ContinueWith(tt => _acceptingSuggestionFlag = false, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        /// <summary>
        /// Gets the visibility for the result list view.
        /// </summary>
        public bool ResultViewbool
        {
            get
            {
                if (!EnableResultListView)
                {
                    return false;
                }

                if (!EnableIndividualResultDisplay && (SearchViewModel?.SearchMode == SearchResultMode.Single || SearchViewModel?.SelectedResult != null))
                {
                    return false;
                }

                if (SearchViewModel?.Results?.Any() ?? false)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the visibility for the source selection button.
        /// </summary>
        public bool SourceSelectbool
        {
            get
            {
                if (SearchViewModel?.Sources.Count > 1)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion binding support

        #region events

        private static void OnGeoViewPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is SearchView sendingView)
            {
                if (oldValue is GeoView oldGeoView)
                {
                    oldGeoView.ViewpointChanged -= sendingView.GeoView_ViewpointChanged;
                    sendingView._lastUsedGeomodel = null;
                    (oldGeoView as INotifyPropertyChanged).PropertyChanged -= sendingView.HandleMapChange;
                    if (oldGeoView.GraphicsOverlays?.Contains(sendingView._resultOverlay) ?? false)
                    {
                        oldGeoView.GraphicsOverlays.Remove(sendingView._resultOverlay);
                    }
                }

                if (newValue is GeoView newGeoView)
                {
                    (newGeoView as INotifyPropertyChanged).PropertyChanged += sendingView.HandleMapChange;
                    newGeoView.ViewpointChanged += sendingView.GeoView_ViewpointChanged;
                    newGeoView.GraphicsOverlays?.Add(sendingView._resultOverlay);
                }

                _ = sendingView.ConfigureForCurrentMap();
            }
        }

        private static void OnViewModelChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is SearchView sendingView)
            {
                if (oldValue is SearchViewModel oldModel)
                {
                    oldModel.PropertyChanged -= sendingView.SearchViewModel_PropertyChanged;
                    oldModel.Sources.CollectionChanged -= sendingView.Sources_CollectionChanged;
                }

                if (newValue is SearchViewModel newModel)
                {
                    newModel.PropertyChanged += sendingView.SearchViewModel_PropertyChanged;
                    newModel.Sources.CollectionChanged += sendingView.Sources_CollectionChanged;
                }
            }
        }

        private void Sources_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PART_SourceSelectButton.IsVisible = SearchViewModel?.Sources?.Count > 1;
        }

        private void HandleMapChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map) || e.PropertyName == nameof(Scene))
            {
                _ = ConfigureForCurrentMap();
                return;
            }

            // When binding, MapView is unreliable about notifying about map changes, especially when first connecting to the view
            if (e.PropertyName == nameof(MapView.DrawStatus) && _lastUsedGeomodel == null)
            {
                if (GeoView is MapView mv && mv.Map is Map map)
                {
                    _lastUsedGeomodel = map;
                }
                else if (GeoView is SceneView sv && sv.Scene is Scene scene)
                {
                    _lastUsedGeomodel = scene;
                }

                _ = ConfigureForCurrentMap();
            }
        }

        private static void OnEnableResultListViewChanged(BindableObject sender, object? oldValue, object? newValue) =>
            (sender as SearchView)?.NotifyPropertyChange(nameof(ResultViewbool));

        private void SearchViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SearchViewModel.ActivePlaceholder):
                    PART_Entry.Placeholder = SearchViewModel.ActivePlaceholder;
                    break;
                case nameof(SearchViewModel.Suggestions):
                    PART_SuggestionsView.ItemsSource = SearchViewModel.Suggestions;
                    PART_SuggestionsView.IsVisible = (SearchViewModel.Suggestions?.Any() ?? false) && SearchViewModel.Results == null;
                    PART_ResultLabel.IsVisible = SearchViewModel.Suggestions != null && SearchViewModel.Suggestions.Count == 0;
                    break;
                case nameof(SearchViewModel.Results):
                    PART_ResultView.ItemsSource = SearchViewModel.Results;
                    PART_ResultView.IsVisible = ResultViewbool;
                    PART_ResultLabel.IsVisible = SearchViewModel.Results != null && SearchViewModel.Results.Count == 0;
                    PART_SuggestionsView.IsVisible = (SearchViewModel.Suggestions?.Any() ?? false) && SearchViewModel.Results == null;
                    _ = HandleResultsCollectionChanged();
                    break;
                case nameof(SearchViewModel.CurrentQuery):
                    PART_CancelButton.IsVisible = !string.IsNullOrEmpty(SearchViewModel.CurrentQuery);
                    PART_Entry.Text = SearchViewModel.CurrentQuery;
                    _ = HandleQueryChanged();
                    break;
                case nameof(SearchViewModel.SearchMode):
                    HandleSearchModeChanged();
                    break;
                case nameof(SearchViewModel.SelectedResult):
                    PART_ResultView.IsVisible = ResultViewbool;
                    _ = HandleSelectedResultChanged();
                    break;
                case nameof(SearchViewModel.IsEligibleForRequery):
                    PART_RepeatButton.IsVisible = SearchViewModel.IsEligibleForRequery;
                    break;
            }
        }

        private void GeoView_ViewpointChanged(object? sender, EventArgs e) => HandleViewpointChanged();

        /// <summary>
        /// Updates <see cref="SearchViewModel"/> with the current viewpoint.
        /// </summary>
        private void HandleViewpointChanged()
        {
            if (SearchViewModel == null)
            {
                return;
            }

            if (GeoView is MapView mv)
            {
                if (mv.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry is Geometry.Geometry newView)
                {
                    SearchViewModel.QueryCenter = (newView as Envelope)?.GetCenter();
                    SearchViewModel.QueryArea = newView;
                }
            }
            else if (GeoView is SceneView sv)
            {
                var newviewpoint = sv.GetCurrentViewpoint(ViewpointType.CenterAndScale);
                if (newviewpoint?.TargetGeometry is MapPoint mp)
                {
                    SearchViewModel.QueryArea = null;
                    SearchViewModel.QueryCenter = mp;
                }
            }
        }

        /// <summary>
        /// Implements typing delay behavior; it is best to wait for user to finish typing before asking for suggestions.
        /// </summary>
        /// <remarks>
        /// The view XAML is expected to bind to the viewmodel property directly, in such a matter that the query updates every keystroke.
        /// </remarks>
        private async Task HandleQueryChanged()
        {
            if (_waitFlag || _acceptingSuggestionFlag || SearchViewModel == null)
            {
                return;
            }

            _waitFlag = true;
            await Task.Delay(TypingDelayMilliseconds);
            _waitFlag = false;

            await SearchViewModel.UpdateSuggestions();
        }

        private async Task HandleSelectedResultChanged()
        {
            NotifyPropertyChange(nameof(ResultViewbool));

            if (SearchViewModel?.SelectedResult is SearchResult selectedResult)
            {
                _resultOverlay?.Graphics.Clear();
                AddResultToGeoView(selectedResult);

                // Zoom to the feature
                if (selectedResult.SelectionViewpoint != null && GeoView != null && SearchViewModel != null)
                {
                    SearchViewModel.IgnoreAreaChangesFlag = true;
                    await GeoView.SetViewpointAsync(selectedResult.SelectionViewpoint);
                    await Task.Delay(1000);
                    SearchViewModel.IgnoreAreaChangesFlag = false;
                }

                if (GeoView != null && selectedResult.CalloutDefinition != null && selectedResult.GeoElement != null)
                {
                    GeoView.ShowCalloutForGeoElement(selectedResult.GeoElement, new Point(0, 0), selectedResult.CalloutDefinition);
                }
            }
            else
            {
                GeoView?.DismissCallout();
            }
        }

        private async Task HandleResultsCollectionChanged()
        {
            if (SearchViewModel == null)
            {
                return;
            }

            NotifyPropertyChange(nameof(ResultViewbool));

            if (SearchViewModel.Results == null)
            {
                _resultOverlay?.Graphics?.Clear();
            }
            else if (SearchViewModel.SelectedResult == null && GeoView != null)
            {
                _resultOverlay?.Graphics?.Clear();
                foreach (var result in SearchViewModel.Results)
                {
                    AddResultToGeoView(result);
                }

                var zoomableResults = SearchViewModel.Results
                                        .Select(res => res.GeoElement?.Geometry).OfType<Geometry.Geometry>().ToList();

                if (zoomableResults != null && zoomableResults.Count > 1)
                {
                    SearchViewModel.IgnoreAreaChangesFlag = true;
                    var newViewpoint = GeometryEngine.CombineExtents(zoomableResults);
                    if (GeoView is MapView mv)
                    {
                        await mv.SetViewpointGeometryAsync(newViewpoint, MultipleResultZoomBuffer);
                    }
                    else
                    {
                        await GeoView.SetViewpointAsync(new Viewpoint(newViewpoint));
                    }

                    await Task.Delay(1000);
                    SearchViewModel.IgnoreAreaChangesFlag = false;
                }
            }
        }

        private void HandleSearchModeChanged()
        {
            NotifyPropertyChange(nameof(ResultViewbool));
        }

        #endregion events

        #region commands

        /// <summary>
        /// Gets a command that clears the current search.
        /// </summary>
        public ICommand ClearCommand { get; private set; }

        /// <summary>
        /// Gets a command that starts a search with current parameters.
        /// </summary>
        public ICommand SearchCommand { get; private set; }

        /// <summary>
        ///  Gets a command that repeats the last search with new geometry.
        /// </summary>
        public ICommand RepeatSearchHereCommand { get; private set; }

        private void HandleClearSearchCommand()
        {
            SearchViewModel?.CancelSearch();
            SearchViewModel?.ClearSearch();
        }

        private void HandleSearchCommand()
        {
            SearchViewModel?.CommitSearch();
        }

        private void HandleRepeatSearchHereCommand()
        {
            SearchViewModel?.RepeatSearchHere();
        }
        #endregion commands

        #region properties

        public DataTemplate? SuggestionTemplate
        {
            get => GetValue(SuggestionTemplateProperty) as DataTemplate;
            set => SetValue(SuggestionTemplateProperty, value);
        }

        public DataTemplate? ResultTemplate
        {
            get => GetValue(ResultTemplateProperty) as DataTemplate;
            set => SetValue(ResultTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the GeoView associated with this view.
        /// </summary>
        /// <remarks>
        /// If set, <see cref="SearchView"/> will add a graphics overlay for showing results, and will automatically navigate to show search results.
        /// </remarks>
        public GeoView? GeoView
        {
            get => GetValue(GeoViewProperty) as GeoView;
            set => SetValue(GeoViewProperty, value);
        }

        /// <summary>
        /// Gets or sets a message to show when a search completes with no results.
        /// </summary>
        public string? NoResultMessage
        {
            get => GetValue(NoResultMessageProperty) as string;
            set => SetValue(NoResultMessageProperty, value);
        }

        /// <summary>
        /// Gets or sets the viewmodel that implements core search behavior.
        /// </summary>
        public SearchViewModel? SearchViewModel
        {
            get => GetValue(SearchViewModelProperty) as SearchViewModel;
            set => SetValue(SearchViewModelProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="SearchView"/> will automatically configure search settings based on the associated <see cref="GeoView"/>'s <see cref="GeoModel"/>.
        /// </summary>
        public bool EnableAutomaticConfiguration
        {
            get => (bool)GetValue(EnableAutomaticConfigurationProperty);
            set => SetValue(EnableAutomaticConfigurationProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a 'Repeat Search' button will be displayed
        /// when the user pans the map a sufficient amount after a search completes.
        /// </summary>
        /// <remarks>
        /// Some consumer applications will display this button in a separate area of the UI from the search bar, often centered over the map.
        /// This property is intended to allow hiding the default button if using a custom 'Repeat Search' implementation.
        /// See <see cref="RepeatSearchHereCommand"/> and <see cref="SearchViewModel.IsEligibleForRequery"/> to enable a custom button implementation.
        /// </remarks>
        public bool EnableRepeatSearchHereButton
        {
            get => (bool)GetValue(EnableRepeatSearchHereButtonProperty);
            set => SetValue(EnableRepeatSearchHereButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the view will show the selected result.
        /// If false, the result list is hidden automatically when a result is selected.
        /// </summary>
        /// <remarks>
        /// See <see cref="SearchViewModel.SelectedResult"/> to display custom UI for the selected result.
        /// </remarks>
        public bool EnableIndividualResultDisplay
        {
            get => (bool)GetValue(EnableIndividualResultDisplayProperty);
            set => SetValue(EnableIndividualResultDisplayProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the default result list view will be shown.
        /// </summary>
        /// <remarks>
        /// Set this value to false to enable a custom list presentation.
        /// </remarks>
        public bool EnableResultListView
        {
            get => (bool)GetValue(EnableResultListViewProperty);
            set => SetValue(EnableResultListViewProperty, value);
        }

        /// <summary>
        /// Gets or sets the buffer used when zooming to a set of results.
        /// </summary>
        public double MultipleResultZoomBuffer
        {
            get => (double)GetValue(MultipleResultZoomBufferProperty);
            set => SetValue(MultipleResultZoomBufferProperty, value);
        }
        #endregion properties

        #region dependency properties

        public static readonly BindableProperty ResultTemplateProperty =
            BindableProperty.Create(nameof(ResultTemplate), typeof(DataTemplate), typeof(SearchView), null);

        public static readonly BindableProperty SuggestionTemplateProperty =
            BindableProperty.Create(nameof(SuggestionTemplate), typeof(DataTemplate), typeof(SearchView), null);

        /// <summary>
        /// Identifies the <see cref="NoResultMessage"/> dependency property.
        /// </summary>
        public static readonly BindableProperty NoResultMessageProperty =
            BindableProperty.Create(nameof(NoResultMessage), typeof(string), typeof(SearchView), null);

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(SearchView), null, propertyChanged: OnGeoViewPropertyChanged);

        /// <summary>
        /// Identifies the <see cref="EnableAutomaticConfiguration"/> dependency property.
        /// </summary>
        public static readonly BindableProperty EnableAutomaticConfigurationProperty =
            BindableProperty.Create(nameof(EnableAutomaticConfiguration), typeof(bool), typeof(SearchView), true);

        /// <summary>
        /// Identifies the <see cref="EnableRepeatSearchHereButton"/> dependency proeprty.
        /// </summary>
        public static readonly BindableProperty EnableRepeatSearchHereButtonProperty =
            BindableProperty.Create(nameof(EnableRepeatSearchHereButton), typeof(bool), typeof(SearchView), true);

        /// <summary>
        /// Identifies the <see cref="SearchViewModel"/> dependency property.
        /// </summary>
        public static readonly BindableProperty SearchViewModelProperty =
            BindableProperty.Create(nameof(SearchViewModel), typeof(SearchViewModel), typeof(SearchView), null, propertyChanged: OnViewModelChanged);

        /// <summary>
        /// Identifies the <see cref="EnableResultListView"/> dependency property.
        /// </summary>
        public static readonly BindableProperty EnableResultListViewProperty =
            BindableProperty.Create(nameof(EnableResultListView), typeof(bool), typeof(SearchView), true, propertyChanged: OnEnableResultListViewChanged);

        /// <summary>
        /// Identifies the <see cref="EnableIndividualResultDisplay"/> dependency property.
        /// </summary>
        public static readonly BindableProperty EnableIndividualResultDisplayProperty =
            BindableProperty.Create(nameof(EnableIndividualResultDisplay), typeof(bool), typeof(SearchView), false, propertyChanged: OnEnableResultListViewChanged);

        /// <summary>
        /// Identifies the <see cref="MultipleResultZoomBuffer"/> dependency property.
        /// </summary>
        public static readonly BindableProperty MultipleResultZoomBufferProperty =
            BindableProperty.Create(nameof(MultipleResultZoomBuffer), typeof(double), typeof(SearchView), 64.0);
        #endregion dependency properties

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChange(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
