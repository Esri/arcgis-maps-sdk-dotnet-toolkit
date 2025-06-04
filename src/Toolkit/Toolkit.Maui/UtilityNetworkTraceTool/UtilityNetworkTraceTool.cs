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
using System.Collections.Specialized;
using System.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;
using Grid = Microsoft.Maui.Controls.Grid;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Represents a control that enables user to perform trace analysis with pre-configured trace types.
/// </summary>
public partial class UtilityNetworkTraceTool : TemplatedView
{
    private CancellationTokenSource? _identifyLayersCts;
    private readonly UtilityNetworkTraceToolController _controller;
    private List<GraphicsOverlay> _resultOverlays = new List<GraphicsOverlay>();

    /// <summary>
    /// Initializes a new instance of the <see cref="UtilityNetworkTraceTool"/> class.
    /// </summary>
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
        _controller.StartingPoints.CollectionChanged += StartingPoints_CollectionChanged;
        _controller.UtilityNetworks.CollectionChanged += UtilityNetworks_CollectionChanged;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (PART_ListViewNetworks != null)
        {
            PART_ListViewNetworks.SelectedIndexChanged -= PART_NetworksCollectionView_SelectionChanged;
            PART_ListViewNetworks.ItemsSource = null;
        }

        if (PART_ListViewTraceTypes != null)
        {
            PART_ListViewTraceTypes.SelectedIndexChanged -= OnTraceTypeSelected;
            PART_ListViewTraceTypes.ItemsSource = null;
        }

        if (PART_ButtonAddStartingPoint != null)
        {
            PART_ButtonAddStartingPoint.Clicked -= OnAddStartingPointClicked;
        }

        if (PART_ListViewStartingPoints != null)
        {
            PART_ListViewStartingPoints.SelectionChanged -= OnStartingPointSelected;
            PART_ListViewStartingPoints.ItemsSource = null;
        }

        if (PART_ButtonRunTrace != null)
        {
            PART_ButtonRunTrace.Clicked -= PART_RunTraceButton_Clicked;
        }

        if (PART_NavigationSegment != null)
        {
            PART_NavigationSegment.PropertyChanged -= OnSectionNavigated;
        }

        if (PART_ButtonCancelAddStartingPoint != null)
        {
            PART_ButtonCancelAddStartingPoint.Clicked -= OnAddStartingPointCancelClicked;
        }

        if (PART_ButtonCancelActivity != null)
        {
            PART_ButtonCancelActivity.Clicked -= PART_CancelWaitButton_Clicked;
        }

        base.OnApplyTemplate();

        if (GetTemplateChild(nameof(PART_LabelNetworks)) is Label networkListLabel)
        {
            PART_LabelNetworks = networkListLabel;
        }

        if (GetTemplateChild(nameof(PART_ListViewNetworks)) is Picker networksCollectionView)
        {
            PART_ListViewNetworks = networksCollectionView;

            PART_ListViewNetworks.ItemsSource = _controller.UtilityNetworks;
            PART_ListViewNetworks.SelectedItem = _controller.SelectedUtilityNetwork;
            PART_ListViewNetworks.SelectedIndexChanged += PART_NetworksCollectionView_SelectionChanged;
        }

        if (GetTemplateChild(nameof(PART_LabelTraceTypes)) is Label traceTypesLabel)
        {
            PART_LabelTraceTypes = traceTypesLabel;
        }

        if (GetTemplateChild(nameof(PART_ButtonAddStartingPoint)) is Button startingPointButton)
        {
            PART_ButtonAddStartingPoint = startingPointButton;
            PART_ButtonAddStartingPoint.Clicked += OnAddStartingPointClicked;
        }

        if (GetTemplateChild(nameof(PART_ListViewStartingPoints)) is CollectionView startingPointListView)
        {
            PART_ListViewStartingPoints = startingPointListView;
            PART_ListViewStartingPoints.ItemsSource = _controller.StartingPoints;
            PART_ListViewStartingPoints.SelectionChanged += OnStartingPointSelected;
        }

        if (GetTemplateChild(nameof(PART_ButtonRunTrace)) is Button runTraceButton)
        {
            PART_ButtonRunTrace = runTraceButton;
            PART_ButtonRunTrace.Clicked += PART_RunTraceButton_Clicked;
        }

        if (GetTemplateChild(nameof(PART_NavigationSegment)) is SegmentedControl navSegment)
        {
            PART_NavigationSegment = navSegment;
            PART_NavigationSegment.PropertyChanged += OnSectionNavigated;
        }

        if (GetTemplateChild(nameof(PART_ButtonCancelAddStartingPoint)) is Button cancelAdd)
        {
            PART_ButtonCancelAddStartingPoint = cancelAdd;
            PART_ButtonCancelAddStartingPoint.Clicked += OnAddStartingPointCancelClicked;
        }

        if (GetTemplateChild(nameof(PART_DuplicateTraceWarning)) is View duplicateWarning)
        {
            PART_DuplicateTraceWarning = duplicateWarning;
        }

        if (GetTemplateChild(nameof(PART_ExtraStartingPointsWarning)) is View extraPointsWarning)
        {
            PART_ExtraStartingPointsWarning = extraPointsWarning;
        }

        if (GetTemplateChild(nameof(PART_NeedMoreStartingPointsWarning)) is View needMorePointsWarning)
        {
            PART_NeedMoreStartingPointsWarning = needMorePointsWarning;
        }

        if (GetTemplateChild(nameof(PART_NoNetworksWarning)) is View noNetworksWarning)
        {
            PART_NoNetworksWarning = noNetworksWarning;
        }

        if (GetTemplateChild(nameof(PART_NoResultsWarning)) is View noResultsWarning)
        {
            PART_NoResultsWarning = noResultsWarning;
        }

        if (GetTemplateChild(nameof(PART_GridResultsDisplay)) is Grid uwpResultGrid)
        {
            PART_GridResultsDisplay = uwpResultGrid;
            BindableLayout.SetItemsSource(PART_GridResultsDisplay, _controller.Results);
        }

        if (GetTemplateChild(nameof(PART_ListViewTraceTypes)) is Picker picker)
        {
            PART_ListViewTraceTypes = picker;
            PART_ListViewTraceTypes.ItemsSource = _controller.TraceTypes;
            PART_ListViewTraceTypes.SelectedIndexChanged += OnTraceTypeSelected;
        }

        if (GetTemplateChild(nameof(PART_ActivityIndicator)) is Border activityIndicator)
        {
            PART_ActivityIndicator = activityIndicator;
        }

        if (GetTemplateChild(nameof(PART_ButtonCancelActivity)) is Button cancelButton)
        {
            PART_ButtonCancelActivity = cancelButton;
            PART_ButtonCancelActivity.Clicked += PART_CancelWaitButton_Clicked;
        }

        if (GetTemplateChild(nameof(PART_SelectContainer)) is Layout selectContainer)
        {
            PART_SelectContainer = selectContainer;
        }
        if (GetTemplateChild(nameof(PART_ConfigureContainer)) is Layout configureContainer)
        {
            PART_ConfigureContainer = configureContainer;
        }
        if (GetTemplateChild(nameof(PART_RunContainer)) is Layout runContainer)
        {
            PART_RunContainer = runContainer;
        }
        if (GetTemplateChild(nameof(PART_ViewContainer)) is Layout viewContainer)
        {
            PART_ViewContainer = viewContainer;
        }



        ApplySegmentLayout();
    }

    private void PART_CancelWaitButton_Clicked(object? sender, EventArgs e)
    {
        _controller?._traceCts?.Cancel();
        _controller?._getFeaturesForElementsCts?.Cancel();
        _identifyLayersCts?.Cancel();
    }

    private void OnStartingPointSelected(object? sender, SelectionChangedEventArgs e) => _controller.SelectedStartingPoint = e.CurrentSelection.FirstOrDefault() as StartingPointModel;

    private void OnTraceTypeSelected(object? sender, EventArgs e)
    {
        if (PART_ListViewTraceTypes?.SelectedItem is UtilityNamedTraceConfiguration config)
        {
            _controller.SelectedTraceType = config;
        }
    }

    private void OnAddStartingPointCancelClicked(object? sender, EventArgs e) => _controller.IsAddingStartingPoints = false;

    private void OnSectionNavigated(object? sender, PropertyChangedEventArgs e)
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
            PART_ConfigureContainer?.SetValue(IsVisibleProperty, false);
            PART_RunContainer?.SetValue(IsVisibleProperty, false);
            PART_SelectContainer?.SetValue(IsVisibleProperty, false);
            PART_ViewContainer?.SetValue(IsVisibleProperty, false);
            return;
        }
        PART_ConfigureContainer?.SetValue(IsVisibleProperty, false);
        PART_RunContainer?.SetValue(IsVisibleProperty, false);
        PART_SelectContainer?.SetValue(IsVisibleProperty, false);
        PART_ViewContainer?.SetValue(IsVisibleProperty, false);
        PART_NoNetworksWarning?.SetValue(IsVisibleProperty, false);
        PART_ListViewNetworks?.SetValue(IsVisibleProperty, false);
        PART_NoResultsWarning?.SetValue(IsVisibleProperty, false);
        PART_LabelNetworks?.SetValue(IsVisibleProperty, false);
        PART_DuplicateTraceWarning?.SetValue(IsVisibleProperty, false);
        PART_ButtonRunTrace?.SetValue(IsVisibleProperty, false);
        PART_GridResultsDisplay?.SetValue(View.IsVisibleProperty, false);
        PART_ButtonCancelAddStartingPoint?.SetValue(IsVisibleProperty, false);
        PART_LabelTraceTypes?.SetValue(IsVisibleProperty, false);
        PART_ListViewTraceTypes?.SetValue(IsVisibleProperty, false);
        PART_ButtonAddStartingPoint?.SetValue(IsVisibleProperty, false);
        PART_ListViewStartingPoints?.SetValue(IsVisibleProperty, false);
        PART_ExtraStartingPointsWarning?.SetValue(IsVisibleProperty, false);
        PART_NeedMoreStartingPointsWarning?.SetValue(IsVisibleProperty, false);
        if (PART_NavigationSegment == null)
        {
            return;
        }

        switch (PART_NavigationSegment.SelectedSegmentIndex)
        {
            // Select
            case 0:
                PART_ListViewNetworks?.SetValue(IsVisibleProperty, _controller.UtilityNetworks.Count > 1);
                PART_LabelNetworks?.SetValue(IsVisibleProperty, _controller.UtilityNetworks.Count > 1);
                PART_ListViewTraceTypes?.SetValue(IsVisibleProperty, true);
                PART_LabelTraceTypes?.SetValue(IsVisibleProperty, true);
                PART_SelectContainer?.SetValue(IsVisibleProperty, true);
                break;

            // Configure
            case 1:
                PART_ListViewStartingPoints?.SetValue(IsVisibleProperty, true);
                PART_ButtonCancelAddStartingPoint?.SetValue(IsVisibleProperty, _controller.IsAddingStartingPoints);
                PART_ButtonAddStartingPoint?.SetValue(IsVisibleProperty, !_controller.IsAddingStartingPoints);
                PART_ExtraStartingPointsWarning?.SetValue(IsVisibleProperty, _controller.TooManyStartingPointsWarning);
                PART_NeedMoreStartingPointsWarning?.SetValue(IsVisibleProperty, _controller.InsufficientStartingPointsWarning);
                PART_ConfigureContainer?.SetValue(IsVisibleProperty, true);
                break;

            // Run
            case 2:
                PART_ButtonRunTrace?.SetValue(IsVisibleProperty, true);
                PART_DuplicateTraceWarning?.SetValue(IsVisibleProperty, _controller.DuplicatedTraceWarning);
                PART_ExtraStartingPointsWarning?.SetValue(IsVisibleProperty, _controller.TooManyStartingPointsWarning);
                PART_NeedMoreStartingPointsWarning?.SetValue(IsVisibleProperty, _controller.InsufficientStartingPointsWarning);
                PART_RunContainer?.SetValue(IsVisibleProperty, true);
                break;

            // Results
            case 3:
                PART_GridResultsDisplay?.SetValue(View.IsVisibleProperty, _controller.Results.Any());
                PART_NoResultsWarning?.SetValue(IsVisibleProperty, !_controller.Results.Any());
                PART_ViewContainer?.SetValue(IsVisibleProperty, true);
                GeoView?.DismissCallout();
                break;
        }
    }

    private void PART_RunTraceButton_Clicked(object? sender, EventArgs e)
    {
        if (_controller.EnableTrace)
        {
            var list = _controller.Results.ToList();
            foreach (var item in list)
            {
                _controller.Results.Remove(item);
            }

            _ = _controller.TraceAsync();
        }
    }

    private void OnAddStartingPointClicked(object? sender, EventArgs e) => _controller.IsAddingStartingPoints = !_controller.IsAddingStartingPoints;

    private void PART_NetworksCollectionView_SelectionChanged(object? sender, EventArgs e)
    {
        _controller.SelectedUtilityNetwork = PART_ListViewNetworks?.SelectedItem as UtilityNetwork;
        UtilityNetworkChanged?.Invoke(this, new UtilityNetworkChangedEventArgs(_controller.SelectedUtilityNetwork));
    }

    private void UtilityNetworks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PART_LabelNetworks?.SetValue(IsVisibleProperty, _controller.UtilityNetworks.Count > 1);
    }

    private void StartingPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => GeoView?.DismissCallout();

    private void Results_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                                
                if (item.Error != null)
                {
                    UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(item.Parameters, item.Error));
                }
                else
                {
                    UtilityNetworkTraceCompleted?.Invoke(this, new UtilityNetworkTraceCompletedEventArgs(item.Parameters, item.RawResults));
                }
            }
        }

        if (_controller.Results.Any())
        {
            PART_NavigationSegment?.SetValue(SegmentedControl.SelectedSegmentIndexProperty, 3);
        }
    }

    private void Controller_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_controller.IsAddingStartingPoints):
                if (_controller.IsAddingStartingPoints)
                {
                    PART_NavigationSegment?.SetValue(SegmentedControl.SelectedSegmentIndexProperty, 1);
                    PART_ButtonAddStartingPoint?.SetValue(IsVisibleProperty, false);
                    PART_ListViewStartingPoints?.SetValue(IsVisibleProperty, false);
                    PART_ButtonCancelAddStartingPoint?.SetValue(IsVisibleProperty, true);
                }
                else
                {
                    PART_ButtonAddStartingPoint?.SetValue(IsVisibleProperty, true);
                    PART_ListViewStartingPoints?.SetValue(IsVisibleProperty, true);
                    PART_ButtonCancelAddStartingPoint?.SetValue(IsVisibleProperty, false);
                }

                break;
            case nameof(_controller.EnableTrace):
                PART_ButtonRunTrace?.SetValue(IsEnabledProperty, _controller.EnableTrace);
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
                PART_ActivityIndicator?.SetValue(IsVisibleProperty, _controller.IsRunningTrace || _controller.IsLoadingNetwork || _identifyLayersCts != null);
                PART_ButtonCancelActivity?.SetValue(IsVisibleProperty, _controller.IsRunningTrace || _identifyLayersCts != null);
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
        if (e?.PropertyName == nameof(MapView.Map) && GeoView is MapView mv)
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
            if (sender is GeoView geoView)
            {
                if (_identifyLayersCts != null)
                {
                    _identifyLayersCts.Cancel();
                }

                PART_ActivityIndicator?.SetValue(IsVisibleProperty, _controller.IsRunningTrace || _controller.IsLoadingNetwork || _identifyLayersCts != null);
                PART_ButtonCancelActivity?.SetValue(IsVisibleProperty, _controller.IsRunningTrace || _identifyLayersCts != null);

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
            _identifyLayersCts = null;
            PART_ActivityIndicator?.SetValue(IsVisibleProperty, _controller.IsRunningTrace || _controller.IsLoadingNetwork || _identifyLayersCts != null);
            PART_ButtonCancelActivity?.SetValue(IsVisibleProperty, _controller.IsRunningTrace || _identifyLayersCts != null);
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

    /// <summary>
    /// Gets or sets a <see cref="Symbol"/> that represents a starting point.
    /// </summary>
    /// <value>
    /// A <see cref="Symbol"/> that represents a starting point.
    /// </value>
    public Symbol? StartingPointSymbol
    {
        get => GetValue(StartingPointSymbolProperty) as Symbol;
        set => SetValue(StartingPointSymbolProperty, value);
    }

    /// <summary>
    /// Gets or sets a <see cref="Symbol"/> that represents an aggregated multipoint trace result.
    /// </summary>
    /// <value>
    /// A <see cref="Symbol"/> that represents an aggregated multipoint trace result.
    /// </value>
    public Symbol? ResultPointSymbol
    {
        get => GetValue(ResultPointSymbolProperty) as Symbol;
        set => SetValue(ResultPointSymbolProperty, value);
    }

    /// <summary>
    /// Gets or sets a <see cref="Symbol"/> that represents an aggregated polyline trace result.
    /// </summary>
    /// <value>
    /// A <see cref="Symbol"/> that represents an aggregated polyline trace result.
    /// </value>
    public Symbol? ResultLineSymbol
    {
        get => GetValue(ResultLineSymbolProperty) as Symbol;
        set => SetValue(ResultLineSymbolProperty, value);
    }

    /// <summary>
    /// Gets or sets a <see cref="Symbol"/> that represents an aggregated polygon trace result.
    /// </summary>
    /// <value>
    /// A <see cref="Symbol"/> that represents an aggregated polygon trace result.
    /// </value>
    public Symbol? ResultFillSymbol
    {
        get => GetValue(ResultFillSymbolProperty) as Symbol;
        set => SetValue(ResultFillSymbolProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the GeoView should automatically zoom to show trace results.
    /// </summary>
    public bool AutoZoomToTraceResults
    {
        get => (bool)GetValue(AutoZoomToTraceResultsProperty);
        set => SetValue(AutoZoomToTraceResultsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="AutoZoomToTraceResults"/> bindable property.
    /// </summary>
    public static readonly BindableProperty AutoZoomToTraceResultsProperty =
        BindableProperty.Create(nameof(AutoZoomToTraceResults), typeof(bool), typeof(UtilityNetworkTraceTool), true, propertyChanged: OnAutoZoomPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="ResultLineSymbol"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ResultLineSymbolProperty =
        BindableProperty.Create(nameof(ResultLineSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnResultLineSymbolPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="ResultPointSymbol"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ResultPointSymbolProperty =
        BindableProperty.Create(nameof(ResultPointSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnResultPointSymbolPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="StartingPointSymbol"/> bindable property.
    /// </summary>
    public static readonly BindableProperty StartingPointSymbolProperty =
        BindableProperty.Create(nameof(StartingPointSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnStartingPointSymbolPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="ResultFillSymbol"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ResultFillSymbolProperty =
        BindableProperty.Create(nameof(ResultFillSymbol), typeof(Symbol), typeof(UtilityNetworkTraceTool), propertyChanged: OnResultFillSymbolPropertyChanged);

    private static void OnAutoZoomPropertyChanged(BindableObject sender, object? oldValue, object? newValue)
    {
        if (sender is UtilityNetworkTraceTool untt && untt._controller is UtilityNetworkTraceToolController controller && newValue != null)
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
