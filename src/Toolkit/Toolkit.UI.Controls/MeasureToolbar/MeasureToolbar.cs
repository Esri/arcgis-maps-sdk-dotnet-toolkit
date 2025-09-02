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

#if WPF || WINDOWS_XAML
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Editing;
#if WPF
using System.Windows.Controls.Primitives;
#endif
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The MeasureToolbar control is used to measure distances and areas on a <see cref="MapView"/>.
    /// </summary>
    [TemplatePart(Name = "MeasureLength", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "MeasureArea", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "MeasureResult", Type = typeof(TextBlock))]
    public partial class MeasureToolbar : Control
    {
        // Supported measure mode
        private enum MeasureToolbarMode
        {
            None,
            Line,
            Area,
            Feature,
        }

        private MeasureToolbarMode _mode = MeasureToolbarMode.None;

        private MeasureToolbarMode Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    PrepareMeasureMode();
                }
            }
        }

        // Used for selecting measure mode
        private ToggleButton? _measureLengthButton;
        private ToggleButton? _measureAreaButton;
        private ToggleButton? _measureFeatureButton;

        // Used for displaying and configuring measurement result
        private TextBlock? _measureResultTextBlock;
        private UIElement? _linearUnitsSelector;
        private UIElement? _areaUnitsSelector;

        // Used for clearing map and measurement result
        private ButtonBase? _clearButton;

        // Used for internal measure editors
        private GeometryEditor? _geometryEditor;

        // Used for restoring original tool when switching from feature measure mode
        private GeometryEditorTool? _originalTool;

        // Commands for undo/redo
        private readonly ICommand _undoCommand;
        private readonly ICommand _redoCommand;

        // Used for highlighting feature for measurement
        private readonly GraphicsOverlay _measureFeatureResultOverlay = new GraphicsOverlay();

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasureToolbar"/> class.
        /// </summary>
        public MeasureToolbar()
        {
            DefaultStyleKey = typeof(MeasureToolbar);

            LinearUnits = new ObservableCollection<LinearUnit>()
                {
                    Geometry.LinearUnits.Centimeters,
                    Geometry.LinearUnits.Feet,
                    Geometry.LinearUnits.Inches,
                    Geometry.LinearUnits.Kilometers,
                    Geometry.LinearUnits.Meters,
                    Geometry.LinearUnits.Miles,
                    Geometry.LinearUnits.Millimeters,
                    Geometry.LinearUnits.NauticalMiles,
                    Geometry.LinearUnits.Yards,
                };
            AreaUnits = new ObservableCollection<AreaUnit>()
                {
                    Geometry.AreaUnits.Acres,
                    Geometry.AreaUnits.Hectares,
                    Geometry.AreaUnits.SquareCentimeters,
                    Geometry.AreaUnits.SquareDecimeters,
                    Geometry.AreaUnits.SquareFeet,
                    Geometry.AreaUnits.SquareKilometers,
                    Geometry.AreaUnits.SquareMeters,
                    Geometry.AreaUnits.SquareMiles,
                    Geometry.AreaUnits.SquareMillimeters,
                    Geometry.AreaUnits.SquareYards,
                };

            _undoCommand = new DelegateCommand(_ => _geometryEditor?.Undo());
            _redoCommand = new DelegateCommand(_ => _geometryEditor?.Redo());
        }

        /// <inheritdoc/>
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            _measureLengthButton = GetTemplateChild("MeasureLength") as ToggleButton;
            if (_measureLengthButton != null)
            {
                _measureLengthButton.Click += OnToggleMeasureMode;
            }

            _measureAreaButton = GetTemplateChild("MeasureArea") as ToggleButton;
            if (_measureAreaButton != null)
            {
                _measureAreaButton.Click += OnToggleMeasureMode;
            }

            _measureFeatureButton = GetTemplateChild("MeasureFeature") as ToggleButton;
            if (_measureFeatureButton != null)
            {
                _measureFeatureButton.Click += OnToggleMeasureMode;
            }

            _clearButton = GetTemplateChild("Clear") as ButtonBase;
            if (_clearButton != null)
            {
                _measureFeatureResultOverlay.Graphics.CollectionChanged += (s, e) =>
                  {
                      if (Mode == MeasureToolbarMode.Feature)
                      {
                          _clearButton.IsEnabled = _measureFeatureResultOverlay.Graphics.Any();
                          AddMeasureFeatureResultOverlay(MapView);
                      }
                  };
                _clearButton.Click += OnClear;
            }

            _measureResultTextBlock = GetTemplateChild("MeasureResult") as TextBlock;
            _linearUnitsSelector = GetTemplateChild("LinearUnitsSelector") as UIElement;
            _areaUnitsSelector = GetTemplateChild("AreaUnitsSelector") as UIElement;

            // Base default unit selection on the culture the app is running under
            var useMetric = !"en-us".Equals(Language.ToString(), StringComparison.OrdinalIgnoreCase);

            if (SelectedLinearUnit == null)
            {
                if (useMetric)
                {
                    SelectedLinearUnit = LinearUnits?.Any(u => u == Geometry.LinearUnits.Meters) == true ?
                    Geometry.LinearUnits.Meters :
                    LinearUnits?.FirstOrDefault();
                }
                else
                {
                    SelectedLinearUnit = LinearUnits?.Any(u => u == Geometry.LinearUnits.Feet) == true ?
                    Geometry.LinearUnits.Feet :
                    LinearUnits?.FirstOrDefault();
                }
            }

            if (SelectedAreaUnit == null)
            {
                if (useMetric)
                {
                    SelectedAreaUnit = AreaUnits?.Any(u => u == Geometry.AreaUnits.SquareKilometers) == true ?
                    Geometry.AreaUnits.SquareKilometers :
                    AreaUnits?.FirstOrDefault();
                }
                else
                {
                    SelectedAreaUnit = AreaUnits?.Any(u => u == Geometry.AreaUnits.SquareMiles) == true ?
                        Geometry.AreaUnits.SquareMiles :
                        AreaUnits?.FirstOrDefault();
                }
            }

            PrepareMeasureMode();
        }

        /// <summary>
        /// Updates UI based on measure mode.
        /// - Only one of the measure toggle buttons is enabled
        /// - Only one of the units selector is visible
        /// - Updates instruction text
        /// - Assigns the correct GeometryEditor tool for the selected mode.
        /// - Updates command to execute on clear.
        /// </summary>
        private void PrepareMeasureMode()
        {
            var isMeasuringLength = _mode == MeasureToolbarMode.Line;
            var isMeasuringArea = _mode == MeasureToolbarMode.Area;
            var isMeasuringFeature = _mode == MeasureToolbarMode.Feature;

            if (MapView != null)
            {
                if (_geometryEditor is GeometryEditor geometryEditor)
                {
                    if (isMeasuringLength || isMeasuringArea)
                    {
                        // Save the original tool before switching
                        _originalTool ??= geometryEditor.Tool;
                        geometryEditor.Tool = MeasureTool ?? _originalTool;
                        geometryEditor.IsVisible = true;
                    }
                    else
                    {
                        // Restore the original tool when not measuring
                        if (_originalTool != null)
                        {
                            geometryEditor.Tool = _originalTool;
                            _originalTool = null;
                        }
                        geometryEditor.IsVisible = false;
                    }
                }

                if (isMeasuringFeature)
                {
                    AddMeasureFeatureResultOverlay(MapView);
                }
                else
                {
                    RemoveMeasureFeatureResultOverlay(MapView);
                }
            }

            if (_measureLengthButton != null)
            {
                _measureLengthButton.IsChecked = isMeasuringLength;
            }

            if (_measureAreaButton != null)
            {
                _measureAreaButton.IsChecked = isMeasuringArea;
            }

            if (_measureFeatureButton != null)
            {
                _measureFeatureButton.IsChecked = isMeasuringFeature;
            }

            if (_linearUnitsSelector != null)
            {
                _linearUnitsSelector.Visibility = !isMeasuringArea ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_areaUnitsSelector != null)
            {
                _areaUnitsSelector.Visibility = isMeasuringArea ? Visibility.Visible : Visibility.Collapsed;
            }

            DisplayResult();

            if (_clearButton != null)
            {
                if (isMeasuringLength || isMeasuringArea)
                {
                    _clearButton.IsEnabled = _geometryEditor?.Geometry != null;
                }
                else
                {
                    _clearButton.IsEnabled = _measureFeatureResultOverlay.Graphics.Any();
                }
            }
        }

        /// <summary>
        /// Updates visibility of unit selector based on geometry type.
        /// </summary>
        /// <param name="geometry">geometry to measure.</param>
        private void PrepareUnitSelector(Geometry.Geometry? geometry)
        {
            var isMeasuringArea = geometry is Polygon || geometry is Envelope;
            if (_linearUnitsSelector != null)
            {
                _linearUnitsSelector.Visibility = !isMeasuringArea ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_areaUnitsSelector != null)
            {
                _areaUnitsSelector.Visibility = isMeasuringArea ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Displays the measurement result.
        /// </summary>
        /// <param name="geometry">geometry to measure.</param>
        private void DisplayResult(Geometry.Geometry? geometry = null)
        {
            if (_measureResultTextBlock != null)
            {
                double measurement = 0;
                if (geometry == null)
                {
                    switch (Mode)
                    {
                        case MeasureToolbarMode.Line:
                        case MeasureToolbarMode.Area:
                            {
                                geometry = _geometryEditor?.Geometry;
                                break;
                            }

                        case MeasureToolbarMode.Feature:
                            {
                                geometry = _measureFeatureResultOverlay.Graphics.FirstOrDefault()?.Geometry;
                                break;
                            }
                    }
                }

                if (geometry is Polyline)
                {
                    measurement = GeometryEngine.LengthGeodetic(geometry, SelectedLinearUnit, GeodeticCurveType.ShapePreserving);
                }
                else if (geometry is Polygon || geometry is Envelope)
                {
                    measurement = GeometryEngine.AreaGeodetic(geometry, SelectedAreaUnit, GeodeticCurveType.ShapePreserving);
                }

                if (geometry == null)
                {
                    var instruction = Mode == MeasureToolbarMode.None ?
                        "Toggle a measure mode" : (Mode == MeasureToolbarMode.Feature ? "Tap a feature" : "Tap to sketch");
                    _measureResultTextBlock.Text = instruction;
                }
                else
                {
                    _measureResultTextBlock.Text = string.Format("{0:0,0.00}", measurement);
                }
            }
        }

        /// <summary>
        /// Toggles between measure modes and starts <see cref="GeometryEditor"/> when not already started for length and area.
        /// </summary>
        /// <param name="sender">Toggle button that raised click event.</param>
        /// <param name="e">Contains information or event data associated with routed event.</param>
        private void OnToggleMeasureMode(object? sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            Mode = toggleButton != null && toggleButton.IsChecked.HasValue && toggleButton.IsChecked.Value ?
               (toggleButton == _measureLengthButton ? MeasureToolbarMode.Line :
               toggleButton == _measureAreaButton ? MeasureToolbarMode.Area :
               toggleButton == _measureFeatureButton ? MeasureToolbarMode.Feature : MeasureToolbarMode.None) :
               MeasureToolbarMode.None;
            if (_geometryEditor != null && (Mode == MeasureToolbarMode.Line || Mode == MeasureToolbarMode.Area))
            {
                try
                {
                    var creationMode = Mode == MeasureToolbarMode.Line ? GeometryType.Polyline : GeometryType.Polygon;
                    _geometryEditor.Start(creationMode);
                    DisplayResult(_geometryEditor.Geometry);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message, ex.GetType().Name);
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the <see cref="GeometryEditor"/>
        /// instance.
        /// </summary>
        /// <remarks>This method updates the state of the clear button based on the <see
        /// cref="GeometryEditor.Geometry"/> property and displays the updated geometry result. The <paramref
        /// name="sender"/> must be a <see cref="GeometryEditor"/>  instance for the method to function
        /// correctly.</remarks>
        /// <param name="sender">The source of the event, expected to be a <see cref="GeometryEditor"/> instance.</param>
        /// <param name="e">The event data containing the name of the property that changed.</param>
        private void OnGeometryEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(GeometryEditor.Geometry))
            {
                var editor = sender as GeometryEditor;
                if (_clearButton != null)
                {
                    _clearButton.IsEnabled = editor?.Geometry is Geometry.Geometry geometry && !geometry.IsEmpty;
                }
                DisplayResult(editor?.Geometry);
            }
            if (_geometryEditor is not null)
            {
                (UndoCommand as DelegateCommand)?.NotifyCanExecuteChanged(_geometryEditor?.CanUndo is true);
                (RedoCommand as DelegateCommand)?.NotifyCanExecuteChanged(_geometryEditor?.CanRedo is true);
            }
        }

        /// <summary>
        /// Identifies the polyline or polygon feature or graphic whose geometry will be measured.
        /// </summary>
        /// <param name="sender">MapView that raised GeoViewTapped event.</param>
        /// <param name="e">Data for the GeoViewTapped event.</param>
        private async void OnMapViewTapped(object? sender, GeoViewInputEventArgs e)
        {
            if (Mode != MeasureToolbarMode.Feature || MapView is null)
            {
                return;
            }

            var identifyLayersResult = await MapView.IdentifyLayersAsync(e.Position, 2, false);
            var geometry = GetGeometry(identifyLayersResult);
            if (geometry == null)
            {
                var identifyGraphicsOverlaysResult = await MapView.IdentifyGraphicsOverlaysAsync(e.Position, 2, false);
                geometry = GetGeometry(identifyGraphicsOverlaysResult);
            }

            PrepareUnitSelector(geometry);
            Symbology.Symbol? symbol = null;
            if (geometry is Polyline)
            {
                symbol = MeasureTool.Style.LineSymbol;
            }
            else if (geometry is Polygon || geometry is Envelope)
            {
                symbol = MeasureTool.Style.FillSymbol;
            }

            if (geometry != null)
            {
                var graphic = _measureFeatureResultOverlay.Graphics.FirstOrDefault();
                if (graphic == null)
                {
                    _measureFeatureResultOverlay.Graphics.Add(new Graphic(geometry, symbol) { IsSelected = true });
                }
                else
                {
                    graphic.Symbol = symbol;
                    graphic.Geometry = geometry;
                }
            }

            DisplayResult(geometry);
        }

        private void RemoveMeasureFeatureResultOverlay(MapView? mapView)
        {
            if (mapView?.GraphicsOverlays != null && mapView.GraphicsOverlays.Contains(_measureFeatureResultOverlay))
            {
                mapView.GraphicsOverlays.Remove(_measureFeatureResultOverlay);
            }
        }

        private void AddMeasureFeatureResultOverlay(MapView? mapView)
        {
            if (mapView == null || !_measureFeatureResultOverlay.Graphics.Any())
            {
                return;
            }

            if (mapView.GraphicsOverlays == null)
            {
                mapView.GraphicsOverlays = new GraphicsOverlayCollection();
            }

            if (!mapView.GraphicsOverlays.Contains(_measureFeatureResultOverlay))
            {
                mapView.GraphicsOverlays.Add(_measureFeatureResultOverlay);
            }
        }

        /// <summary>
        /// Recursively checks SublayerResults and returns the geometry of the first polyline or polygon feature.
        /// </summary>
        /// <param name="identifyLayerResults">Results returned from identifying layers.</param>
        /// <returns>the first polyline or polygon geometry.</returns>
        private Geometry.Geometry? GetGeometry(IEnumerable<IdentifyLayerResult> identifyLayerResults)
        {
            foreach (var result in identifyLayerResults)
            {
                foreach (var geoElement in result.GeoElements)
                {
                    if (geoElement.Geometry is Polyline || geoElement.Geometry is Polygon)
                    {
                        return geoElement.Geometry;
                    }
                }

                var geometry = GetGeometry(result.SublayerResults);
                if (geometry != null)
                {
                    return geometry;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the geometry of the first polyline or polygon graphic.
        /// </summary>
        /// <param name="identifyGraphicsOverlayResults">Results returned from identifying graphics.</param>
        /// <returns>the first polyline or polygon geometry.</returns>
        private Geometry.Geometry? GetGeometry(IEnumerable<IdentifyGraphicsOverlayResult> identifyGraphicsOverlayResults)
        {
            foreach (var result in identifyGraphicsOverlayResults)
            {
                foreach (var graphic in result.Graphics)
                {
                    if (graphic.Geometry is Polyline || graphic.Geometry is Polygon)
                    {
                        return graphic.Geometry;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Clears the map of any graphics from measuring distance, area or feature.
        /// This will also clear undo/redo stack.
        /// </summary>
        /// <param name="sender">Button that raised clicked event.</param>
        /// <param name="e">Contains information or event data associated with routed event.</param>
        private void OnClear(object? sender, RoutedEventArgs e)
        {
            if (Mode == MeasureToolbarMode.Line || Mode == MeasureToolbarMode.Area)
            {
                _geometryEditor?.ClearGeometry();
            }
            else if (Mode == MeasureToolbarMode.Feature)
            {
                _measureFeatureResultOverlay.Graphics.Clear();
            }

            DisplayResult();
        }

        /// <summary>
        /// Gets or sets the map view where measuring distances and areas will be done.
        /// </summary>
        public MapView? MapView
        {
            get { return GetValue(MapViewProperty) as MapView; }
            set { SetValue(MapViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MapView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MapViewProperty =
            DependencyProperty.Register(nameof(MapView), typeof(MapView), typeof(MeasureToolbar), new PropertyMetadata(null, OnMapViewPropertyChanged));

        private static void OnMapViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            if (e.NewValue is not MapView newMapView)
            {
                throw new ArgumentException($"{nameof(MapView)} cannot be null or empty.");
            }

            if (e.OldValue is MapView oldMapView)
            {
                oldMapView.GeoViewTapped -= toolbar.OnMapViewTapped;
                toolbar.RemoveMeasureFeatureResultOverlay(oldMapView);
            }

            newMapView.GeoViewTapped += toolbar.OnMapViewTapped;
            toolbar._geometryEditor = newMapView.GeometryEditor;
            if (toolbar._geometryEditor != null)
            {
                toolbar._geometryEditor.PropertyChanged += toolbar.OnGeometryEditorPropertyChanged;
            }
            toolbar.DisplayResult(newMapView.GeometryEditor?.Geometry);
        }

        /// <summary>
        /// Gets the <see cref="VertexTool"/> used for measuring distances and areas.
        /// </summary>
        public VertexTool MeasureTool { get; } = new VertexTool
        {
            Style = new GeometryEditorStyle
            {
                FillSymbol = new Symbology.SimpleFillSymbol
                {
                    Color = System.Drawing.Color.FromArgb(90, 60, 60, 60),
                }
            }
        };

        /// <summary>
        /// Gets or sets the collection of <see cref="Geometry.LinearUnit"/> used to configure display for distance measurements.
        /// </summary>
        public IList<LinearUnit>? LinearUnits
        {
            get { return GetValue(LinearUnitsProperty) as IList<LinearUnit>; }
            set { SetValue(LinearUnitsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LinearUnits"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LinearUnitsProperty =
            DependencyProperty.Register(nameof(LinearUnits), typeof(IList<LinearUnit>), typeof(MeasureToolbar), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the unit used to display for distance measurements.
        /// </summary>
        public LinearUnit? SelectedLinearUnit
        {
            get { return GetValue(SelectedLinearUnitProperty) as LinearUnit; }
            set { SetValue(SelectedLinearUnitProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedLinearUnit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedLinearUnitProperty =
            DependencyProperty.Register(nameof(SelectedLinearUnit), typeof(LinearUnit), typeof(MeasureToolbar), new PropertyMetadata(null, OnSelectedLinearUnitPropertyChanged));

        private static void OnSelectedLinearUnitPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            toolbar.DisplayResult();
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="Geometry.AreaUnit"/> used to configure display for area measurements.
        /// </summary>
        public IList<AreaUnit>? AreaUnits
        {
            get { return GetValue(AreaUnitsProperty) as IList<AreaUnit>; }
            set { SetValue(AreaUnitsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AreaUnits"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreaUnitsProperty =
            DependencyProperty.Register(nameof(AreaUnits), typeof(IList<AreaUnit>), typeof(MeasureToolbar), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the unit used to display for area measurements.
        /// </summary>
        public AreaUnit? SelectedAreaUnit
        {
            get { return GetValue(SelectedAreaUnitProperty) as AreaUnit; }
            set { SetValue(SelectedAreaUnitProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedAreaUnit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedAreaUnitProperty =
            DependencyProperty.Register(nameof(SelectedAreaUnit), typeof(AreaUnit), typeof(MeasureToolbar), new PropertyMetadata(null, OnSelectedAreaUnitPropertyChanged));

        private static void OnSelectedAreaUnitPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            toolbar.DisplayResult();
        }

        /// <summary>
        /// Gets a command that undoes the last operation performed in the geometry editor.
        /// </summary>
        public ICommand? UndoCommand => _undoCommand;

        /// <summary>
        /// Gets a command that redoes the last operation performed in the geometry editor.
        /// </summary>
        public ICommand? RedoCommand => _redoCommand;
    }
}
#endif