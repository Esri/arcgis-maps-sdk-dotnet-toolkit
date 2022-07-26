using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Primitives;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Grid = Xamarin.Forms.Grid;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    public partial class UtilityNetworkTraceTool : TemplatedView
    {
        private CancellationTokenSource? _identifyLayersCts;
        private readonly UtilityNetworkTraceToolController _controller;
        private List<GraphicsOverlay> _resultOverlays = new List<GraphicsOverlay>();

        public UtilityNetworkTraceTool()
        {
            _controller = new UtilityNetworkTraceToolController();
            ResultFillSymbol = _controller.ResultFillSymbol;
            ResultLineSymbol = _controller.ResultLineSymbol;
            ResultPointSymbol = _controller.ResultPointSymbol;
            StartingPointSymbol = _controller.StartingPointSymbol;
            ControlTemplate = DefaultControlTemplate;
            _controller.AutoZoomToTraceResults = AutoZoomToTraceResults;
            _controller.PropertyChanged += Controller_PropertyChanged;
            _controller.Results.CollectionChanged += Results_CollectionChanged;
            _controller.TraceTypes.CollectionChanged += TraceTypes_CollectionChanged;
            _controller.StartingPoints.CollectionChanged += StartingPoints_CollectionChanged;
            _controller.UtilityNetworks.CollectionChanged += UtilityNetworks_CollectionChanged;
        }

        protected override void OnApplyTemplate()
        {
            if (PART_NetworksCollectionView != null)
            {
                PART_NetworksCollectionView.ItemSelected -= PART_NetworksCollectionView_SelectionChanged;
                PART_NetworksCollectionView.ItemsSource = null;
            }
            if (PART_TraceTypesListView != null)
            {
                PART_TraceTypesListView.ItemSelected -= PART_TraceTypesListView_ItemSelected;
                PART_TraceTypesListView.ItemsSource = null;
            }

            if (PART_AddStartingPointButton != null)
            {
                PART_AddStartingPointButton.Clicked -= PART_AddStartingPointButton_Clicked;
            }
            if (PART_StartingPointListViewUWP != null)
            {
                PART_StartingPointListViewUWP.ItemSelected -= PART_StartingPointListViewUWP_ItemSelected;
                PART_StartingPointListViewUWP.ItemsSource = null;
            }
            if (PART_RunTraceButton != null)
            {
                PART_RunTraceButton.Clicked -= PART_RunTraceButton_Clicked;
            }
            if (PART_NavigationSegment != null)
            {
                PART_NavigationSegment.PropertyChanged -= PART_NavigationSegment_PropertyChanged;
            }
            if (PART_CancelAddStartingPointButton != null)
            {
                PART_CancelAddStartingPointButton.Clicked -= PART_CancelAddStartingPointButton_Clicked;
            }
            if(PART_CancelWaitButton != null)
            {
                PART_CancelWaitButton.Clicked -= PART_CancelWaitButton_Clicked;
            }

            base.OnApplyTemplate();

            if (GetTemplateChild(nameof(PART_NetworksListLabel)) is Label _networkListLabel)
            {
                PART_NetworksListLabel = _networkListLabel;
            }

            if (GetTemplateChild(nameof(PART_NetworksCollectionView)) is ListView _networksCollectionView)
            {
                PART_NetworksCollectionView = _networksCollectionView;

                PART_NetworksCollectionView.ItemsSource = _controller.UtilityNetworks;
                PART_NetworksCollectionView.SelectedItem = _controller.SelectedUtilityNetwork;
                PART_NetworksCollectionView.ItemSelected += PART_NetworksCollectionView_SelectionChanged;
            }

            if (GetTemplateChild(nameof(PART_TraceTypesLabel)) is Label traceTypesLabel)
            {
                PART_TraceTypesLabel = traceTypesLabel;
            }

            if (GetTemplateChild(nameof(PART_AddStartingPointButton)) is Button startingPointButton)
            {
                PART_AddStartingPointButton = startingPointButton;
                PART_AddStartingPointButton.Clicked += PART_AddStartingPointButton_Clicked;
            }
            if (GetTemplateChild(nameof(PART_StartingPointListViewUWP)) is ListView startingPointListView)
            {
                PART_StartingPointListViewUWP = startingPointListView;
                PART_StartingPointListViewUWP.ItemsSource = _controller.StartingPoints;
                PART_StartingPointListViewUWP.ItemSelected += PART_StartingPointListViewUWP_ItemSelected;
            }
            if (GetTemplateChild(nameof(PART_RunTraceButton)) is Button runTraceButton)
            {
                PART_RunTraceButton = runTraceButton;
                PART_RunTraceButton.Clicked += PART_RunTraceButton_Clicked;
            }
            if (GetTemplateChild(nameof(PART_NavigationSegment)) is SegmentedControl navSegment)
            {
                PART_NavigationSegment = navSegment;
                PART_NavigationSegment.PropertyChanged += PART_NavigationSegment_PropertyChanged;
            }
            if (GetTemplateChild(nameof(PART_CancelAddStartingPointButton)) is Button cancelAdd)
            {
                PART_CancelAddStartingPointButton = cancelAdd;
                PART_CancelAddStartingPointButton.Clicked += PART_CancelAddStartingPointButton_Clicked;
            }
            if (GetTemplateChild(nameof(PART_DuplicateTraceWarningContainer)) is View duplicateWarning)
            {
                PART_DuplicateTraceWarningContainer = duplicateWarning;
            }
            if (GetTemplateChild(nameof(PART_ExtraStartingPointsWarningContainer)) is View extraPointsWarning)
            {
                PART_ExtraStartingPointsWarningContainer = extraPointsWarning;
            }
            if (GetTemplateChild(nameof(PART_NeedMoreStartingPointsWarningContainer)) is View needMorePointsWarning)
            {
                PART_NeedMoreStartingPointsWarningContainer = needMorePointsWarning;
            }

            if (GetTemplateChild(nameof(PART_NoNetworksWarning)) is View noNetworksWarning)
            {
                PART_NoNetworksWarning = noNetworksWarning;
            }
            if (GetTemplateChild(nameof(PART_NoResultsWarning)) is View noResultsWarning)
            {
                PART_NoResultsWarning = noResultsWarning;
            }
            if (GetTemplateChild(nameof(PART_ResultDisplayUWP)) is Grid uwpResultGrid)
            {
                PART_ResultDisplayUWP = uwpResultGrid;
                BindableLayout.SetItemsSource(PART_ResultDisplayUWP, _controller.Results);
            }
            if (GetTemplateChild(nameof(PART_TraceTypesListView)) is ListView listView)
            {
                PART_TraceTypesListView = listView;
                PART_TraceTypesListView.ItemsSource = _controller.TraceTypes;
                PART_TraceTypesListView.ItemSelected += PART_TraceTypesListView_ItemSelected;
            }
            if(GetTemplateChild(nameof(PART_ActivityIndicator)) is Frame activityIndicator)
            {
                PART_ActivityIndicator = activityIndicator;
            }
            if(GetTemplateChild(nameof(PART_CancelWaitButton)) is Button cancelButton)
            {
                PART_CancelWaitButton = cancelButton;
                PART_CancelWaitButton.Clicked += PART_CancelWaitButton_Clicked;
            }

            ApplySegmentLayout();
        }

        private void PART_CancelWaitButton_Clicked(object sender, EventArgs e)
        {
            _controller._traceCts.Cancel();
            _controller._getFeaturesForElementsCts.Cancel();
        }

        private void PART_StartingPointListViewUWP_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _controller.SelectedStartingPoint = e.SelectedItem as StartingPointModel;
        }

        private void PART_TraceTypesListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _controller.SelectedTraceType = e.SelectedItem as UtilityNamedTraceConfiguration;
        }

        private void PART_CancelAddStartingPointButton_Clicked(object sender, EventArgs e)
        {
            _controller.IsAddingStartingPoints = false;
        }

        private void PART_NavigationSegment_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SegmentedControl.SelectedSegmentIndex))
            {
                ApplySegmentLayout();
            }
        }

        private void ApplySegmentLayout()
        {
            if (!_controller.UtilityNetworks.Any())
            {
                PART_NoNetworksWarning?.SetValue(IsVisibleProperty, true);
                PART_NetworksCollectionView?.SetValue(IsVisibleProperty, false);
                PART_NetworksListLabel?.SetValue(IsVisibleProperty, false);
                PART_TraceTypesListView?.SetValue(IsVisibleProperty, false);
                PART_TraceTypesLabel?.SetValue(IsVisibleProperty, false);
                PART_AddStartingPointButton?.SetValue(IsVisibleProperty, false);
                PART_StartingPointListViewUWP?.SetValue(IsVisibleProperty, false);
                PART_RunTraceButton?.SetValue(IsVisibleProperty, false);
                PART_ResultDisplayUWP?.SetValue(View.IsVisibleProperty, false);
                PART_CancelAddStartingPointButton?.SetValue(IsVisibleProperty, false);
                PART_ExtraStartingPointsWarningContainer?.SetValue(IsVisibleProperty, false);
                PART_NeedMoreStartingPointsWarningContainer?.SetValue(IsVisibleProperty, false);
                PART_DuplicateTraceWarningContainer?.SetValue(IsVisibleProperty, false);
                return;
            }
            PART_NoNetworksWarning?.SetValue(IsVisibleProperty, false);
            PART_NetworksCollectionView?.SetValue(IsVisibleProperty, false);
            PART_NoResultsWarning?.SetValue(IsVisibleProperty, false);
            PART_NetworksListLabel?.SetValue(IsVisibleProperty, false);
            PART_DuplicateTraceWarningContainer?.SetValue(IsVisibleProperty, false);
            PART_RunTraceButton?.SetValue(IsVisibleProperty, false);
            PART_ResultDisplayUWP?.SetValue(View.IsVisibleProperty, false);
            PART_CancelAddStartingPointButton?.SetValue(IsVisibleProperty, false);
            PART_TraceTypesLabel?.SetValue(IsVisibleProperty, false);
            PART_TraceTypesListView?.SetValue(IsVisibleProperty, false);
            PART_AddStartingPointButton?.SetValue(IsVisibleProperty, false);
            PART_StartingPointListViewUWP?.SetValue(IsVisibleProperty, false);
            PART_ExtraStartingPointsWarningContainer?.SetValue(IsVisibleProperty, false);
            PART_NeedMoreStartingPointsWarningContainer?.SetValue(IsVisibleProperty, false);
            switch (PART_NavigationSegment.SelectedSegmentIndex)
            {
                // Select
                case 0:
                    PART_NetworksCollectionView?.SetValue(IsVisibleProperty, _controller.UtilityNetworks.Count > 1);
                    PART_NetworksListLabel?.SetValue(IsVisibleProperty, _controller.UtilityNetworks.Count > 1);
                    PART_TraceTypesListView?.SetValue(IsVisibleProperty, true);
                    PART_TraceTypesLabel?.SetValue(IsVisibleProperty, true);
                    break;
                // Configure
                case 1:
                    PART_StartingPointListViewUWP?.SetValue(IsVisibleProperty, true);
                    PART_CancelAddStartingPointButton?.SetValue(IsVisibleProperty, _controller.IsAddingStartingPoints);
                    PART_AddStartingPointButton?.SetValue(IsVisibleProperty, !_controller.IsAddingStartingPoints);
                    PART_ExtraStartingPointsWarningContainer?.SetValue(IsVisibleProperty, _controller.TooManyStartingPointsWarning);
                    PART_NeedMoreStartingPointsWarningContainer?.SetValue(IsVisibleProperty, _controller.InsufficientStartingPointsWarning);
                    break;
                // Run
                case 2:
                    PART_RunTraceButton?.SetValue(IsVisibleProperty, true);
                    PART_DuplicateTraceWarningContainer?.SetValue(IsVisibleProperty, _controller.DuplicatedTraceWarning);
                    PART_ExtraStartingPointsWarningContainer?.SetValue(IsVisibleProperty, _controller.TooManyStartingPointsWarning);
                    PART_NeedMoreStartingPointsWarningContainer?.SetValue(IsVisibleProperty, _controller.InsufficientStartingPointsWarning);
                    break;
                // Results
                case 3:
                    PART_ResultDisplayUWP?.SetValue(View.IsVisibleProperty, _controller.Results.Any());
                    PART_NoResultsWarning?.SetValue(IsVisibleProperty, !_controller.Results.Any());

                    if (GeoView != null)
                    {
                        GeoView.DismissCallout();
                    }
                    break;
            }
        }

        private void PART_RunTraceButton_Clicked(object sender, EventArgs e)
        {
            if (_controller.EnableTrace)
            {
                if (Device.RuntimePlatform == Device.UWP)
                {
                    var list = _controller.Results.ToList();
                    foreach (var item in list)
                    {
                        _controller.Results.Remove(item);
                    }
                    //_controller.Results.Clear();
                }
                _ = _controller.TraceAsync();
            }
        }

        private void PART_AddStartingPointButton_Clicked(object sender, EventArgs e)
        {
            _controller.IsAddingStartingPoints = !_controller.IsAddingStartingPoints;
        }

        private void PART_TraceTypesCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _controller.SelectedTraceType = e.CurrentSelection.FirstOrDefault() as UtilityNamedTraceConfiguration;
        }

        private void PART_NetworksCollectionView_SelectionChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is UtilityNetwork newSelection)
            {
                //_controller.SelectedUtilityNetwork = newSelection;
            }
        }

        private void UtilityNetworks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_controller.UtilityNetworks.Count > 1)
            {
                PART_NetworksListLabel?.SetValue(IsVisibleProperty, true);
            }
            //throw new NotImplementedException();
        }

        private void StartingPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (GeoView != null)
            {
                GeoView.DismissCallout();
            }
            //throw new NotImplementedException();
        }

        private void TraceTypes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Results_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset && GeoView?.GraphicsOverlays is GraphicsOverlayCollection visualizedOverlays)
            {
                foreach (var item in _resultOverlays)
                {
                    if (visualizedOverlays.Contains(item))
                    {
                        visualizedOverlays.Remove(item);
                    }
                }

                _resultOverlays = new List<GraphicsOverlay>();

                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<UtilityTraceOperationResult>())
                    {
                        item.AreFeaturesSelected = false;
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<UtilityTraceOperationResult>())
                {
                    if (GeoView?.GraphicsOverlays?.Contains(item.ResultOverlay) ?? false)
                    {
                        GeoView.GraphicsOverlays.Remove(item.ResultOverlay);
                    }

                    item.AreFeaturesSelected = false;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<UtilityTraceOperationResult>())
                {
                    if (!(GeoView?.GraphicsOverlays?.Contains(item.ResultOverlay) ?? true))
                    {
                        _resultOverlays.Add(item.ResultOverlay);
                        GeoView.GraphicsOverlays.Insert(0, item.ResultOverlay);
                    }

                    UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(item.Parameters, item.RawResults));
                    if (item?.Error != null)
                    {
                        UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(item.Parameters, item.Error));
                    }
                }
            }

            if (_controller.Results.Any())
            {
                PART_NavigationSegment.SelectedSegmentIndex = 3;
                SetValue(BottomSheetLayout.LayoutPreferenceProperty, "5050");
            }
        }

        private void Controller_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_controller.IsAddingStartingPoints):
                    if (_controller.IsAddingStartingPoints)
                    {
                        PART_NavigationSegment.SelectedSegmentIndex = 1;
                        PART_AddStartingPointButton?.SetValue(IsVisibleProperty, false);
                        PART_StartingPointListViewUWP?.SetValue(IsVisibleProperty, false);
                        PART_CancelAddStartingPointButton?.SetValue(IsVisibleProperty, true);
                        SetValue(BottomSheetLayout.LayoutPreferenceProperty, "collapsed");
                    }
                    else
                    {
                        PART_AddStartingPointButton?.SetValue(IsVisibleProperty, true);
                        PART_StartingPointListViewUWP?.SetValue(IsVisibleProperty, true);
                        PART_CancelAddStartingPointButton?.SetValue(IsVisibleProperty, false);
                        SetValue(BottomSheetLayout.LayoutPreferenceProperty, "5050");
                    }
                    break;
                case nameof(_controller.EnableTrace):
                    PART_RunTraceButton?.SetValue(IsEnabledProperty, _controller.EnableTrace);
                    break;
                case nameof(_controller.RequestedViewpoint):
                    if (GeoView != null && _controller.RequestedViewpoint != null)
                    {
                        GeoView.SetViewpoint(_controller.RequestedViewpoint);
                    }
                    break;
                case nameof(_controller.RequestedCallout):
                    if (GeoView != null)
                    {
                        if (_controller.RequestedCallout == null)
                        {
                            GeoView.DismissCallout();
                        }
                        else
                        {
                            GeoView.ShowCalloutAt(_controller.RequestedCallout.Item2, _controller.RequestedCallout.Item1);
                        }
                    }
                    break;
                case nameof(_controller.DuplicatedTraceWarning):
                case nameof(_controller.InsufficientStartingPointsWarning):
                case nameof(_controller.TooManyStartingPointsWarning):
                    ApplySegmentLayout();
                    break;
                case nameof(_controller.IsLoadingNetwork):
                case nameof(_controller.IsRunningTrace):
                    PART_ActivityIndicator?.SetValue(IsVisibleProperty, _controller.IsRunningTrace || _controller.IsLoadingNetwork);
                    PART_CancelWaitButton?.SetValue(IsVisibleProperty, _controller.IsRunningTrace);
                    break;
            }
        }

        private static void OnGeoViewPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is UtilityNetworkTraceTool sendingView)
            {
                sendingView._controller.Reset(true);

                sendingView._controller.IsRunningTrace = true;

                var graphicsOverlays = new[] { sendingView._controller.StartingPointGraphicsOverlay };

                if (oldValue is GeoView oldGeoView)
                {
                    oldGeoView.GeoViewTapped -= sendingView.OnGeoViewTapped;
                    oldGeoView.PropertyChanged -= sendingView.OnGeoModelPropertyChanged;

                    if (oldGeoView.GraphicsOverlays != null)
                    {
                        foreach (var graphicsOverlay in graphicsOverlays)
                        {
                            if (oldGeoView.GraphicsOverlays.Contains(graphicsOverlay))
                            {
                                oldGeoView.GraphicsOverlays.Remove(graphicsOverlay);
                            }
                        }
                    }
                }

                if (newValue is GeoView newGeoView)
                {
                    newGeoView.GeoViewTapped += sendingView.OnGeoViewTapped;
                    newGeoView.PropertyChanged += sendingView.OnGeoModelPropertyChanged;

                    if (newGeoView.GraphicsOverlays != null)
                    {
                        foreach (var graphicsOverlay in graphicsOverlays)
                        {
                            if (!newGeoView.GraphicsOverlays.Contains(graphicsOverlay))
                            {
                                newGeoView.GraphicsOverlays.Add(graphicsOverlay);
                            }
                        }
                    }
                    if (newGeoView is MapView mv && mv.Map != null)
                    {
                        sendingView._controller.Map = mv.Map;
                    }
                }
            }
        }
        private void OnGeoModelPropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            if (e.PropertyName == nameof(MapView.Map) && GeoView is MapView mv)
            {
                _controller.Map = mv.Map;
            }
        }

        private async void OnGeoViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            if (e.Handled || !_controller.IsAddingStartingPoints || _controller.SelectedUtilityNetwork == null)
            {
                return;
            }

            try
            {
                //_part_identifyInProgressIndicator?.SetValue(VisibilityProperty, Visibility.Visible);

                if (sender is GeoView geoView)
                {
                    if (_identifyLayersCts != null)
                    {
                        _identifyLayersCts.Cancel();
                    }

                    _identifyLayersCts = new CancellationTokenSource();
                    var identifyResults = await geoView.IdentifyLayersAsync(e.Position, 10d, false, _identifyLayersCts.Token);

                    foreach (var identifyResult in identifyResults)
                    {
                        if (GetFeature(identifyResult) is ArcGISFeature feature)
                        {
                            _controller.AddStartingPoint(feature, e.Location);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Do nothing when canceled
            }
            catch (Exception)
            {
            }
            finally
            {
                //_part_identifyInProgressIndicator?.SetValue(VisibilityProperty, Visibility.Collapsed);
            }
        }

        private ArcGISFeature? GetFeature(IdentifyLayerResult layerResult)
        {
            foreach (var geoElement in layerResult.GeoElements)
            {
                if (geoElement is ArcGISFeature feature)
                {
                    return feature;
                }
            }

            return GetFeature(layerResult.SublayerResults);
        }

        private ArcGISFeature? GetFeature(IEnumerable<IdentifyLayerResult> layerResults)
        {
            foreach (var layerResult in layerResults)
            {
                if (GetFeature(layerResult) is ArcGISFeature element)
                {
                    return element;
                }
            }

            return null;
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
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(UtilityNetworkTraceTool), null, propertyChanged: OnGeoViewPropertyChanged);

        public Symbol? StartingPointSymbol
        {
            get => GetValue(StartingPointSymbolProperty) as Symbol;
            set => SetValue(StartingPointSymbolProperty, value);
        }
        public Symbol? ResultPointSymbol
        {
            get => GetValue(ResultPointSymbolProperty) as Symbol;
            set => SetValue(ResultPointSymbolProperty, value);
        }
        public Symbol? ResultLineSymbol
        {
            get => GetValue(ResultLineSymbolProperty) as Symbol;
            set => SetValue(ResultLineSymbolProperty, value);
        }
        public Symbol? ResultFillSymbol
        {
            get => GetValue(ResultFillSymbolProperty) as Symbol;
            set => SetValue(ResultFillSymbolProperty, value);
        }
        public bool AutoZoomToTraceResults
        {
            get => (bool)GetValue(AutoZoomToTraceResultsProperty);
            set => SetValue(AutoZoomToTraceResultsProperty, value);
        }
        public static readonly BindableProperty AutoZoomToTraceResultsProperty =
            BindableProperty.Create(nameof(AutoZoomToTraceResults), typeof(bool), typeof(UtilityNetworkTraceTool), true, propertyChanged: OnAutoZoomPropertyChanged);
        public static readonly BindableProperty ResultLineSymbolProperty =
            BindableProperty.Create(nameof(ResultLineSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnResultLineSymbolPropertyChanged);
        public static readonly BindableProperty ResultPointSymbolProperty =
            BindableProperty.Create(nameof(ResultPointSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnResultPointSymbolPropertyChanged);
        public static readonly BindableProperty StartingPointSymbolProperty =
            BindableProperty.Create(nameof(StartingPointSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnStartingPointSymbolPropertyChanged);
        public static readonly BindableProperty ResultFillSymbolProperty =
            BindableProperty.Create(nameof(ResultFillSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnResultFillSymbolPropertyChanged);

        private static void OnAutoZoomPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is UtilityNetworkTraceTool untt && untt._controller is UtilityNetworkTraceToolController controller)
            {
                controller.AutoZoomToTraceResults = (bool)newValue;
            }
        }
        private static void OnStartingPointSymbolPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is UtilityNetworkTraceTool untt && untt._controller is UtilityNetworkTraceToolController controller)
            {
                controller.StartingPointSymbol = newValue as Symbol;
            }
        }
        private static void OnResultPointSymbolPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is UtilityNetworkTraceTool untt && untt._controller is UtilityNetworkTraceToolController controller)
            {
                controller.ResultPointSymbol = newValue as Symbol;
            }
        }
        private static void OnResultLineSymbolPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is UtilityNetworkTraceTool untt && untt._controller is UtilityNetworkTraceToolController controller)
            {
                controller.ResultLineSymbol = newValue as Symbol;
            }
        }
        private static void OnResultFillSymbolPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            if (sender is UtilityNetworkTraceTool untt && untt._controller is UtilityNetworkTraceToolController controller)
            {
                controller.ResultFillSymbol = newValue as Symbol;
            }
        }
        /// <summary>
        /// Event raised when a new utility network is selected.
        /// </summary>
        public event EventHandler<UtilityNetworkChangedEventArgs>? UtilityNetworkChanged;

        /// <summary>
        /// Event raised when a utility network trace is completed.
        /// </summary>
        public event EventHandler<UtilityNetworkTraceCompletedEventArgs>? UtilityNetworkTraceCompleted;

    }
}

