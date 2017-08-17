// /*******************************************************************************
//  * Copyright 2012-2016 Esri
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
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;

#if NETFX_CORE
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The MeasureToolbar control is used to measure distances and areas on a <see cref="MapView"/>.
    /// </summary>
    public class MeasureToolbar : Control
    {
        // Supported measure mode
        private enum MeasureToolbarMode
        {
            None,
            Line,
            Area,
            Feature
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
        private ToggleButton _measureLengthButton;
        private ToggleButton _measureAreaButton;
        private ToggleButton _measureFeatureButton;

        // Used for displaying and configuring measurement result
        private TextBlock _measureResultTextBlock;
        private UIElement _linearUnitsSelector;
        private UIElement _areaUnitsSelector;

        // Used for clearing map and measurement result
        private ButtonBase _clearButton;

        // Used for replacing measure editors
        private SketchEditor _originalSketchEditor;

        // Used for highlighting feature for measurement
        private readonly GraphicsOverlay _measureFeatureResultOverlay = new GraphicsOverlay() { Id = "MeasureFeatureResultOverlay" };

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasureToolbar"/> class.
        /// </summary>
        public MeasureToolbar()
        {
            DefaultStyleKey = typeof(MeasureToolbar);
        }

        /// <inheritdoc/>
#if NETFX_CORE
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
                      }
                  };
                _clearButton.Click += OnClear;
            }

            _measureResultTextBlock = GetTemplateChild("MeasureResult") as TextBlock;
            if (_measureResultTextBlock != null)
            {
                _measureResultTextBlock.Text = "Toggle a measure mode";
            }

            _linearUnitsSelector = GetTemplateChild("LinearUnitsSelector") as UIElement;
            _areaUnitsSelector = GetTemplateChild("AreaUnitsSelector") as UIElement;
            if (LinearUnits == null || !LinearUnits.Any())
            {
                LinearUnits = new LinearUnit[]
                {
                    Geometry.LinearUnits.Centimeters,
                    Geometry.LinearUnits.Feet,
                    Geometry.LinearUnits.Inches,
                    Geometry.LinearUnits.Kilometers,
                    Geometry.LinearUnits.Meters,
                    Geometry.LinearUnits.Miles,
                    Geometry.LinearUnits.Millimeters,
                    Geometry.LinearUnits.NauticalMiles,
                    Geometry.LinearUnits.Yards
                };
            }

            if (SelectedLinearUnit == null)
            {
                SelectedLinearUnit = Geometry.LinearUnits.Meters;
            }

            if (AreaUnits == null || !AreaUnits.Any())
            {
                AreaUnits = new AreaUnit[]
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
                    Geometry.AreaUnits.SquareYards
                };
            }

            if (SelectedAreaUnit == null)
            {
                SelectedAreaUnit = Geometry.AreaUnits.SquareMiles;
            }

            if (LineSketchEditor == null)
            {
                LineSketchEditor = new SketchEditor();
            }

            if (AreaSketchEditor == null)
            {
                AreaSketchEditor = new SketchEditor();
            }

            if (SelectionLineSymbol == null)
            {
                SelectionLineSymbol = LineSketchEditor?.Style?.LineSymbol;
            }

            if (SelectionFillSymbol == null)
            {
                SelectionFillSymbol = AreaSketchEditor?.Style?.FillSymbol;
            }

            Mode = MeasureToolbarMode.None;
        }

        /// <summary>
        /// Updates UI based on measure mode.
        /// - Only one of the measure toggle buttons is enabled
        /// - Only one of the units selector is visible
        /// - Updates instruction text
        /// - Assigns the appropriate SketchEditor
        /// - Updates command to execute on clear
        /// </summary>
        private void PrepareMeasureMode()
        {
            var isMeasuringLength = _mode == MeasureToolbarMode.Line;
            var isMeasuringArea = _mode == MeasureToolbarMode.Area;
            var isMeasuringFeature = _mode == MeasureToolbarMode.Feature;

            var sketchEditor = isMeasuringLength ?
                LineSketchEditor :
                (isMeasuringArea ? AreaSketchEditor : _originalSketchEditor);
            if (MapView.SketchEditor != sketchEditor)
            {
                MapView.SketchEditor = sketchEditor;
            }

            MapView.SketchEditor.IsVisible = isMeasuringLength || isMeasuringArea;
            _measureFeatureResultOverlay.IsVisible = isMeasuringFeature;

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
                    Binding binding = new Binding();
                    binding.Path = new PropertyPath(nameof(SketchEditor.CancelCommand));
                    _clearButton.DataContext = MapView.SketchEditor;
                    _clearButton.SetBinding(ButtonBase.CommandProperty, binding);
                }
                else
                {
                    _clearButton.ClearValue(ButtonBase.CommandProperty);
                }
            }
        }

        /// <summary>
        /// Updates visibility of unit selector based on geometry type.
        /// </summary>
        /// <param name="geometry">geometry to measure</param>
        private void PrepareUnitSelector(Geometry.Geometry geometry)
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
        /// Displays the measurement result
        /// </summary>
        /// <param name="geometry">geometry to measure</param>
        private void DisplayResult(Geometry.Geometry geometry = null)
        {
            if (_measureResultTextBlock != null)
            {
                double measurement = 0;
                if (geometry == null)
                {
                    switch (Mode)
                    {
                        case MeasureToolbarMode.Line:
                            {
                                geometry = LineSketchEditor.Geometry;
                                break;
                            }

                        case MeasureToolbarMode.Area:
                            {
                                geometry = AreaSketchEditor.Geometry;
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

                if (measurement == 0)
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
        /// Toggles between measure modes and starts SketchEditor when not already started for length and area.
        /// </summary>
        /// <param name="sender">Toggle button that raised click event</param>
        /// <param name="e">Contains information or event data associated with routed event</param>
        private async void OnToggleMeasureMode(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            Mode = toggleButton.IsChecked.HasValue && toggleButton.IsChecked.Value ?
               (toggleButton == _measureLengthButton ? MeasureToolbarMode.Line :
               toggleButton == _measureAreaButton ? MeasureToolbarMode.Area :
               toggleButton == _measureFeatureButton ? MeasureToolbarMode.Feature : MeasureToolbarMode.None) :
               MeasureToolbarMode.None;
            while (Mode == MeasureToolbarMode.Line || Mode == MeasureToolbarMode.Area)
            {
                try
                {
                    if (MapView.SketchEditor.Geometry != null)
                    {
                        break;
                    }

                    var creationMode = Mode == MeasureToolbarMode.Line ? SketchCreationMode.Polyline : SketchCreationMode.Polygon;
                    var geometry = await MapView.SketchEditor.StartAsync(creationMode);
                    DisplayResult(geometry);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message, ex.GetType().Name);
                }

                Mode = toggleButton.IsChecked.HasValue && toggleButton.IsChecked.Value ?
                    (toggleButton == _measureLengthButton ? MeasureToolbarMode.Line :
                    toggleButton == _measureAreaButton ? MeasureToolbarMode.Area :
                    toggleButton == _measureFeatureButton ? MeasureToolbarMode.Feature : MeasureToolbarMode.None) :
                    MeasureToolbarMode.None;
            }
        }

        /// <summary>
        /// Displays the measurement result for the given new geometry.
        /// </summary>
        /// <param name="sender">SketchEditor that raised GeometryChanged event</param>
        /// <param name="e">Data for the GeometryChanged event</param>
        private void OnGeometryChanged(object sender, GeometryChangedEventArgs e)
        {
            DisplayResult(e.NewGeometry);
        }

        /// <summary>
        /// Identifies the polyline or polygon feature or graphic whose geometry will be measured.
        /// </summary>
        /// <param name="sender">MapView that raised GeoViewTapped event</param>
        /// <param name="e">Data for the GeoViewTapped event</param>
        private async void OnMapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (Mode != MeasureToolbarMode.Feature)
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
            Symbology.Symbol symbol = null;
            if (geometry is Polyline)
            {
                symbol = SelectionLineSymbol;
            }
            else if (geometry is Polygon || geometry is Envelope)
            {
                symbol = SelectionFillSymbol;
            }

            var graphic = _measureFeatureResultOverlay.Graphics.FirstOrDefault();
            if (graphic == null)
            {
                _measureFeatureResultOverlay.Graphics.Add(new Graphic(geometry, symbol) { IsSelected = true });
            }
            else
            {
                graphic.Geometry = geometry;
            }

            DisplayResult(geometry);
        }

        /// <summary>
        /// Recursively checks SublayerResults and returns the geometry of the first polyline or polygon feature.
        /// </summary>
        /// <param name="identifyLayerResults">Results returned from identifying layers</param>
        /// <returns>the first polyline or polygon geometry</returns>
        private Geometry.Geometry GetGeometry(IEnumerable<IdentifyLayerResult> identifyLayerResults)
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
        /// <param name="identifyGraphicsOverlayResults">Results returned from identifying graphics</param>
        /// <returns>the first polyline or polygon geometry</returns>
        private Geometry.Geometry GetGeometry(IEnumerable<IdentifyGraphicsOverlayResult> identifyGraphicsOverlayResults)
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
        /// <param name="sender">Button that raised clicked event</param>
        /// <param name="e">Contains information or event data associated with routed event</param>
        private void OnClear(object sender, RoutedEventArgs e)
        {
            if (Mode == MeasureToolbarMode.Line || Mode == MeasureToolbarMode.Area)
            {
                if (MapView.SketchEditor.CancelCommand.CanExecute(null))
                {
                    MapView.SketchEditor.CancelCommand.CanExecute(null);
                }
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
        public MapView MapView
        {
            get { return (MapView)GetValue(MapViewProperty); }
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
            var newMapView = e.NewValue as MapView;
            if (newMapView == null)
            {
                throw new ArgumentException($"{nameof(MapView)} cannot be null or empty.");
            }

            var oldMapView = e.OldValue as MapView;
            if (oldMapView != null)
            {
                oldMapView.SketchEditor = toolbar._originalSketchEditor;
                oldMapView.GeoViewTapped -= toolbar.OnMapViewTapped;
                if (oldMapView.GraphicsOverlays.Any(o => o == toolbar._measureFeatureResultOverlay))
                {
                    oldMapView.GraphicsOverlays.Remove(toolbar._measureFeatureResultOverlay);
                }
            }

            newMapView.GeoViewTapped += toolbar.OnMapViewTapped;
            toolbar._originalSketchEditor = newMapView.SketchEditor;

            if (newMapView.GraphicsOverlays == null)
            {
                newMapView.GraphicsOverlays = new GraphicsOverlayCollection();
            }

            newMapView.GraphicsOverlays.Add(toolbar._measureFeatureResultOverlay);

            toolbar.DisplayResult(newMapView.SketchEditor.Geometry);
        }

        /// <summary>
        /// Gets or sets the sketch editor used for measuring distances.
        /// </summary>
        public SketchEditor LineSketchEditor
        {
            get { return (SketchEditor)GetValue(LineSketchEditorProperty); }
            set { SetValue(LineSketchEditorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LineSketchEditor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineSketchEditorProperty =
            DependencyProperty.Register(nameof(LineSketchEditor), typeof(SketchEditor), typeof(MeasureToolbar), new PropertyMetadata(null, OnLineSketchEditorPropertyChanged));

        private static void OnLineSketchEditorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            var newSketchEditor = e.NewValue as SketchEditor;
            if (newSketchEditor == null)
            {
                throw new ArgumentException($"{nameof(LineSketchEditor)} cannot be null or empty.");
            }

            var oldSketchEditor = e.OldValue as SketchEditor;
            if (oldSketchEditor != null)
            {
                oldSketchEditor.GeometryChanged -= toolbar.OnGeometryChanged;
            }

            newSketchEditor.GeometryChanged += toolbar.OnGeometryChanged;
            toolbar.DisplayResult(newSketchEditor.Geometry);
        }

        /// <summary>
        /// Gets or sets the sketch edtiro used for measuring areas.
        /// </summary>
        public SketchEditor AreaSketchEditor
        {
            get { return (SketchEditor)GetValue(AreaSketchEditorProperty); }
            set { SetValue(AreaSketchEditorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AreaSketchEditor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreaSketchEditorProperty =
            DependencyProperty.Register(nameof(AreaSketchEditor), typeof(SketchEditor), typeof(MeasureToolbar), new PropertyMetadata(null, OnLineSketchEditorPropertyChanged));

        private static void OnAreaSketchEditorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            var newSketchEditor = e.NewValue as SketchEditor;
            if (newSketchEditor == null)
            {
                throw new ArgumentException($"{nameof(AreaSketchEditor)} cannot be null or empty.");
            }

            var oldSketchEditor = e.OldValue as SketchEditor;
            if (oldSketchEditor != null)
            {
                oldSketchEditor.GeometryChanged -= toolbar.OnGeometryChanged;
            }

            newSketchEditor.GeometryChanged += toolbar.OnGeometryChanged;
            toolbar.DisplayResult(newSketchEditor.Geometry);
        }

        /// <summary>
        /// Gets or sets the symbol used for highlighting the polyline feature or graphic whose geometry is measured for distance.
        /// </summary>
        public Symbology.Symbol SelectionLineSymbol
        {
            get { return (Symbology.Symbol)GetValue(SelectionLineSymbolProperty); }
            set { SetValue(SelectionLineSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectionLineSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionLineSymbolProperty =
            DependencyProperty.Register(nameof(SelectionLineSymbol), typeof(Symbology.Symbol), typeof(MeasureToolbar), new PropertyMetadata(null, OnSelectionLineSymbolPropertyChanged));

        private static void OnSelectionLineSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            var symbol = e.NewValue as Symbology.Symbol;
            var graphic = toolbar._measureFeatureResultOverlay.Graphics.FirstOrDefault();
            if (graphic?.Geometry is Polyline)
            {
                graphic.Symbol = symbol;
            }
        }

        /// <summary>
        /// Gets or sets the symbol used for highlighting the polygon feature or graphic whose geometry is measured for area.
        /// </summary>
        public Symbology.Symbol SelectionFillSymbol
        {
            get { return (Symbology.Symbol)GetValue(SelectionFillSymbolProperty); }
            set { SetValue(SelectionFillSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectionFillSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionFillSymbolProperty =
            DependencyProperty.Register(nameof(SelectionFillSymbol), typeof(Symbology.Symbol), typeof(MeasureToolbar), new PropertyMetadata(null, OnSelectionFillSymbolPropertyChanged));

        private static void OnSelectionFillSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            var symbol = e.NewValue as Symbology.Symbol;
            var graphic = toolbar._measureFeatureResultOverlay.Graphics?.FirstOrDefault();
            if (graphic?.Geometry is Polygon || graphic?.Geometry is Envelope)
            {
                graphic.Symbol = symbol;
            }
        }

        /// <summary>
        /// Gets or sets the color used for highlighting the feature or graphic whose geometry is measured for distance or area.
        /// </summary>
        public Color SelectionColor
        {
            get { return (Color)GetValue(SelectionColorProperty); }
            set { SetValue(SelectionColorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectionColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionColorProperty =
            DependencyProperty.Register(nameof(SelectionColor), typeof(Color), typeof(MeasureToolbar), new PropertyMetadata(Colors.Cyan, OnSelectionFillSymbolPropertyChanged));

        private static void OnSelectionColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = (MeasureToolbar)d;
            var newColor = (Color)e.NewValue;
            toolbar._measureFeatureResultOverlay.SelectionColor = newColor;
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="Geometry.LinearUnit"/> used to configure display for distance measurements.
        /// </summary>
        public IEnumerable<LinearUnit> LinearUnits
        {
            get { return (IEnumerable<LinearUnit>)GetValue(LinearUnitsProperty); }
            set { SetValue(LinearUnitsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LinearUnits"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LinearUnitsProperty =
            DependencyProperty.Register(nameof(LinearUnits), typeof(IEnumerable<LinearUnit>), typeof(MeasureToolbar), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the unit used to display for distance measurements.
        /// </summary>
        public LinearUnit SelectedLinearUnit
        {
            get { return (LinearUnit)GetValue(SelectedLinearUnitProperty); }
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
        public IEnumerable<AreaUnit> AreaUnits
        {
            get { return (IEnumerable<AreaUnit>)GetValue(AreaUnitsProperty); }
            set { SetValue(AreaUnitsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AreaUnits"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreaUnitsProperty =
            DependencyProperty.Register(nameof(AreaUnits), typeof(IEnumerable<AreaUnit>), typeof(MeasureToolbar), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the unit used to display for area measurements.
        /// </summary>
        public AreaUnit SelectedAreaUnit
        {
            get { return (AreaUnit)GetValue(SelectedAreaUnitProperty); }
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
    }
}