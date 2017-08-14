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
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The MeasureToolbar control is used to measure distances and areas on a <see cref="MapView"/>.
    /// </summary>
    public class MeasureToolbar : Control
    {
        private enum MeasureToolbarMode
        {
            None,
            Line,
            Area,
            Feature
        }

        private ToggleButton _measureLengthButton;
        private ToggleButton _measureAreaButton;
        private ToggleButton _measureFeatureButton;
        private TextBlock _measureResultTextBlock;
        private ButtonBase _clearButton;
        private UIElement _linearUnitsSelector;
        private UIElement _areaUnitsSelector;

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

        private SketchEditor _originalSketchEditor;
        private Geometry.Geometry _geometry;
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
                _clearButton.Click += OnClear;
            }

            _measureResultTextBlock = GetTemplateChild("MeasureResult") as TextBlock;
            if (_measureResultTextBlock != null)
            {
                _measureResultTextBlock.Text = "Tap to measure.";
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
        }

        private void PrepareMeasureMode()
        {
            switch (_mode)
            {
                case MeasureToolbarMode.None:
                    {
                        if (MapView.SketchEditor != _originalSketchEditor)
                        {
                            MapView.SketchEditor.IsVisible = false;
                            MapView.SketchEditor.IsEnabled = false;
                            MapView.SketchEditor = _originalSketchEditor;
                        }

                        _measureFeatureResultOverlay.IsVisible = false;
                        if (_measureLengthButton != null)
                        {
                            _measureLengthButton.IsChecked = false;
                        }

                        if (_measureAreaButton != null)
                        {
                            _measureAreaButton.IsChecked = false;
                        }

                        if (_measureFeatureButton != null)
                        {
                            _measureFeatureButton.IsChecked = false;
                        }

                        if (_linearUnitsSelector != null)
                        {
                            _linearUnitsSelector.Visibility = Visibility.Visible;
                        }

                        if (_areaUnitsSelector != null)
                        {
                            _areaUnitsSelector.Visibility = Visibility.Collapsed;
                        }

                        if (_measureResultTextBlock != null)
                        {
                            _measureResultTextBlock.Text = "Select measure mode";
                        }

                        break;
                    }

                case MeasureToolbarMode.Line:
                    {
                        if (MapView.SketchEditor != LineSketchEditor)
                        {
                            MapView.SketchEditor.IsVisible = false;
                            MapView.SketchEditor.IsEnabled = false;
                            MapView.SketchEditor = LineSketchEditor;
                        }

                        MapView.SketchEditor.IsVisible = true;
                        MapView.SketchEditor.IsEnabled = true;
                        _measureFeatureResultOverlay.IsVisible = false;
                        if (_measureLengthButton != null)
                        {
                            _measureLengthButton.IsChecked = true;
                        }

                        if (_measureAreaButton != null)
                        {
                            _measureAreaButton.IsChecked = false;
                        }

                        if (_measureFeatureButton != null)
                        {
                            _measureFeatureButton.IsChecked = false;
                        }

                        if (_linearUnitsSelector != null)
                        {
                            _linearUnitsSelector.Visibility = Visibility.Visible;
                        }

                        if (_areaUnitsSelector != null)
                        {
                            _areaUnitsSelector.Visibility = Visibility.Collapsed;
                        }

                        if (LineSketchEditor.Geometry != null)
                        {
                            DisplayResult(LineSketchEditor.Geometry);
                        }
                        else if (_measureResultTextBlock != null)
                        {
                            _measureResultTextBlock.Text = "Tap to sketch line";
                        }

                        break;
                    }

                case MeasureToolbarMode.Area:
                    {
                        if (MapView.SketchEditor != AreaSketchEditor)
                        {
                            MapView.SketchEditor.IsVisible = false;
                            MapView.SketchEditor.IsEnabled = false;
                            MapView.SketchEditor = AreaSketchEditor;
                        }

                        MapView.SketchEditor.IsVisible = true;
                        MapView.SketchEditor.IsEnabled = true;
                        _measureFeatureResultOverlay.IsVisible = false;
                        if (_measureLengthButton != null)
                        {
                            _measureLengthButton.IsChecked = false;
                        }

                        if (_measureAreaButton != null)
                        {
                            _measureAreaButton.IsChecked = true;
                        }

                        if (_measureFeatureButton != null)
                        {
                            _measureFeatureButton.IsChecked = false;
                        }

                        if (_linearUnitsSelector != null)
                        {
                            _linearUnitsSelector.Visibility = Visibility.Collapsed;
                        }

                        if (_areaUnitsSelector != null)
                        {
                            _areaUnitsSelector.Visibility = Visibility.Visible;
                        }

                        if (AreaSketchEditor.Geometry != null)
                        {
                            DisplayResult(AreaSketchEditor.Geometry);
                        }
                        else if (_measureResultTextBlock != null)
                        {
                            _measureResultTextBlock.Text = "Tap to sketch area";
                        }

                        break;
                    }

                case MeasureToolbarMode.Feature:
                    {
                        if (MapView.SketchEditor != _originalSketchEditor)
                        {
                            MapView.SketchEditor.IsVisible = false;
                            MapView.SketchEditor.IsEnabled = false;
                            MapView.SketchEditor = _originalSketchEditor;
                        }

                        _measureFeatureResultOverlay.IsVisible = true;
                        if (_measureLengthButton != null)
                        {
                            _measureLengthButton.IsChecked = false;
                        }

                        if (_measureAreaButton != null)
                        {
                            _measureAreaButton.IsChecked = false;
                        }

                        if (_measureFeatureButton != null)
                        {
                            _measureFeatureButton.IsChecked = true;
                        }

                        if (_linearUnitsSelector != null)
                        {
                            _linearUnitsSelector.Visibility = Visibility.Visible;
                        }

                        if (_areaUnitsSelector != null)
                        {
                            _areaUnitsSelector.Visibility = Visibility.Collapsed;
                        }

                        var geometry = _measureFeatureResultOverlay.Graphics.FirstOrDefault()?.Geometry;
                        if (geometry != null)
                        {
                            DisplayResult(geometry);
                        }
                        else if (_measureResultTextBlock != null)
                        {
                            _measureResultTextBlock.Text = "Tap a feature";
                        }

                        break;
                    }
            }
        }

        private void PrepareUnitSelector(Geometry.Geometry geometry)
        {
            if (geometry is Polygon || geometry is Envelope)
            {
                if (_linearUnitsSelector != null)
                {
                    _linearUnitsSelector.Visibility = Visibility.Collapsed;
                }

                if (_areaUnitsSelector != null)
                {
                    _areaUnitsSelector.Visibility = Visibility.Visible;
                }
            }
            else if (geometry is Polyline)
            {
                if (_linearUnitsSelector != null)
                {
                    _linearUnitsSelector.Visibility = Visibility.Visible;
                }

                if (_areaUnitsSelector != null)
                {
                    _areaUnitsSelector.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void DisplayResult(Geometry.Geometry geometry)
        {
            _geometry = geometry;
            if (_measureResultTextBlock != null)
            {
                double measurement = 0;
                if (geometry is Polyline)
                {
                    measurement = GeometryEngine.LengthGeodetic(geometry, SelectedLinearUnit, GeodeticCurveType.ShapePreserving);
                }

                if (geometry is Polygon || geometry is Envelope)
                {
                    measurement = GeometryEngine.AreaGeodetic(geometry, SelectedAreaUnit, GeodeticCurveType.ShapePreserving);
                }

                _measureResultTextBlock.Text = string.Format("{0:0,0.00}", measurement);
            }
        }

        private async void OnToggleMeasureMode(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            await MeasureAsync(toggleButton);
        }

        private void OnGeometryChanged(object sender, GeometryChangedEventArgs e)
        {
            DisplayResult(e.NewGeometry);
        }

        private async void OnMapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (_clearButton != null)
            {
                if (Mode == MeasureToolbarMode.Line || Mode == MeasureToolbarMode.Area)
                {
                    _clearButton.IsEnabled = MapView.SketchEditor.CancelCommand.CanExecute(null);
                }

                var hasGraphics = _measureFeatureResultOverlay.Graphics.Count > 0;
                _clearButton.IsEnabled = hasGraphics;
            }

            if (Mode != MeasureToolbarMode.Feature)
            {
                return;
            }

            var layerResult = await MapView.IdentifyLayersAsync(e.Position, 2, false);
            var geometry = layerResult.FirstOrDefault()?.GeoElements?.FirstOrDefault()?.Geometry;
            if (geometry == null)
            {
                var graphicResult = await MapView.IdentifyGraphicsOverlaysAsync(e.Position, 2, false);
                geometry = graphicResult.FirstOrDefault()?.Graphics?.FirstOrDefault()?.Geometry;
            }

            PrepareUnitSelector(geometry);
            Symbology.Symbol symbol = null;
            if (geometry is Polyline)
            {
                symbol = SelectionLineSymbol;
            }
            else if (geometry is Polygon || _geometry is Envelope)
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

        private async Task MeasureAsync(ToggleButton toggleButton)
        {
            var isChecked = toggleButton.IsChecked.HasValue && toggleButton.IsChecked.Value;
            Mode = isChecked ?
                (toggleButton == _measureLengthButton ? MeasureToolbarMode.Line :
                toggleButton == _measureAreaButton ? MeasureToolbarMode.Area :
                toggleButton == _measureFeatureButton ? MeasureToolbarMode.Feature : MeasureToolbarMode.None) :
                MeasureToolbarMode.None;
            while (Mode == MeasureToolbarMode.Line || Mode == MeasureToolbarMode.Area)
            {
                try
                {
                    var creationMode = Mode == MeasureToolbarMode.Line ? SketchCreationMode.Polyline : SketchCreationMode.Polygon;
                    if (MapView.SketchEditor.CancelCommand.CanExecute(null))
                    {
                        break;
                    }

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

                isChecked = toggleButton.IsChecked.HasValue && toggleButton.IsChecked.Value;
                Mode = isChecked ?
                    (toggleButton == _measureLengthButton ? MeasureToolbarMode.Line :
                    toggleButton == _measureAreaButton ? MeasureToolbarMode.Area :
                    toggleButton == _measureFeatureButton ? MeasureToolbarMode.Feature : MeasureToolbarMode.None) :
                    MeasureToolbarMode.None;
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            ClearGraphics();
        }

        private void ClearGraphics()
        {
            if (Mode == MeasureToolbarMode.Line || Mode == MeasureToolbarMode.Area)
            {
                if (MapView.SketchEditor.CancelCommand.CanExecute(null))
                {
                    MapView.SketchEditor.CancelCommand.Execute(null);
                }
            }
            else if (Mode == MeasureToolbarMode.Feature)
            {
                _measureFeatureResultOverlay.Graphics.Clear();
            }

            DisplayResult(null);
        }

        /// <summary>
        /// Gets or sets the <see cref="Esri.ArcGISRuntime.UI.Controls.MapView"/> where measuring distances and areas will be done.
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
        /// Gets or sets the <see cref="SketchEditor"/> used for measuring distances.
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
        /// Gets or sets the <see cref="SketchEditor"/> used for measuring areas.
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
        /// Gets or sets the <see cref="Symbology.Symbol"/> used for highlighting the polyline graphic or feature whose geometry is measured for distance.
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
            var graphic = toolbar._measureFeatureResultOverlay.Graphics?.FirstOrDefault();
            if (graphic?.Geometry is Polyline)
            {
                graphic.Symbol = symbol;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Symbology.Symbol"/> used for highlighting the polygon graphic or feature whose geometry is measured for area.
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
        /// Gets or sets the <see cref="Color"/> used for highlighting the graphic or feature whose geometry is measured for area.
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
        /// Gets or sets the <see cref="Geometry.LinearUnit"/> used to display for distance measurements.
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
            toolbar.DisplayResult(toolbar._geometry);
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
        /// Gets or sets the <see cref="Geometry.AreaUnit"/> used to display for area measurements.
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
            toolbar.DisplayResult(toolbar._geometry);
        }
    }
}