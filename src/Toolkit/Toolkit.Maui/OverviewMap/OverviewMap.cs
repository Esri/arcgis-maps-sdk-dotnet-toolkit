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

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Microsoft.Maui.Controls.Internals;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Defines a small "overview" (or "inset") map displaying a representation of the attached <see cref="GeoView"/>'s current viewpoint.
/// </summary>
public class OverviewMap : TemplatedView
{
    private OverviewMapController? _controller;
    private static readonly ControlTemplate DefaultControlTemplate;

    private MapView? _overviewMapView;

    static OverviewMap()
    {
        DefaultControlTemplate = new ControlTemplate(() =>
        {
            var converter = new Internal.LoadStatusToVisibilityConverter();
            Border rootBorder = new Border
            {
                Padding = new Thickness(1),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Stroke = Colors.Black,
                BackgroundColor = Colors.White
            };
            Grid root = new Grid();
            MapView mapView = new MapView()
            {
                IsAttributionTextVisible = false
            };
            ActivityIndicator activity = new ActivityIndicator();
            activity.SetBinding(ActivityIndicator.IsRunningProperty, static (MapView mapView) => mapView.Map?.LoadStatus, converter: converter, converterParameter: "Loading", source: mapView);
            root.Add(activity);
            Label label = new Label()
            {
                TextColor = Colors.Black,
                Text = "Map failed to load. Did you forget an API key?"
            };
            label.SetBinding(VisualElement.IsVisibleProperty, static (MapView mapView) => mapView.Map?.LoadStatus, converter: converter, converterParameter: "FailedToLoad", source: mapView);
            root.Add(label);
            mapView.SetBinding(VisualElement.IsVisibleProperty, static (MapView mapView) => mapView.Map?.LoadStatus, converter: converter, converterParameter: "Loaded", source: mapView);
            root.Add(mapView);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(rootBorder, nameScope);
            nameScope.RegisterName("PART_MapView", mapView);
            rootBorder.Content = root;
            return rootBorder;
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OverviewMap"/> class.
    /// </summary>
    public OverviewMap()
    {
        ControlTemplate = DefaultControlTemplate;
        HeightRequest = 100;
        WidthRequest = 100;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Start;
        AreaSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Null, System.Drawing.Color.Transparent, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 1));
        PointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Red, 16);
        Map = new Map(BasemapStyle.ArcGISTopographic);
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_overviewMapView != null)
        {
            _controller?.Dispose();
            _overviewMapView.Map = null;
        }

        _overviewMapView = GetTemplateChild("PART_MapView") as MapView;

        if (_overviewMapView != null)
        {
            _overviewMapView.Map = Map;

            _controller = new OverviewMapController(_overviewMapView)
            {
                ScaleFactor = ScaleFactor,
                PointSymbol = PointSymbol,
                AreaSymbol = AreaSymbol,
                AttachedView = GeoView,
            };
        }
    }

    /// <summary>
    /// Gets or sets the symbol used to draw the <see cref="GeoView"/>'s visible area.
    /// </summary>
    /// <remarks>
    /// The default is an empty fill symbol with a 1 point red outline.
    /// </remarks>
    public Symbol? AreaSymbol
    {
        get => GetValue(AreaSymbolProperty) as Symbol;
        set => SetValue(AreaSymbolProperty, value);
    }

    /// <summary>
    /// Gets or sets the geoview whose extent is to be displayed.
    /// </summary>
    /// <remarks>
    /// Note that by default interaction with <see cref="OverviewMap"/> will navigate the attached GeoView.
    /// </remarks>
    public GeoView? GeoView
    {
        get => GetValue(GeoViewProperty) as GeoView;
        set => SetValue(GeoViewProperty, value);
    }

    /// <summary>
    /// Gets or sets the Map shown in the inset/overview map.
    /// </summary>
    /// <remarks>
    /// Defaults to a map with a basemap in style <see cref="BasemapStyle.ArcGISTopographic"/>.
    /// </remarks>
    public Map? Map
    {
        get => GetValue(MapProperty) as Map;
        set => SetValue(MapProperty, value);
    }

    /// <summary>
    /// Gets or sets the symbol used to draw the <see cref="GeoView"/>'s viewpoint when it isn't possible to show the visible area (for example, when showing a scene).
    /// </summary>
    /// <remarks>
    /// The default is a red cross.
    /// </remarks>
    public Symbol? PointSymbol
    {
        get => GetValue(PointSymbolProperty) as Symbol;
        set => SetValue(PointSymbolProperty, value);
    }

    /// <summary>
    /// Gets or sets the amount to scale the overview map's viewpoint compared to the attached <see cref="GeoView"/>.
    /// </summary>
    /// <remarks>
    /// The default is 25.
    /// </remarks>
    public double ScaleFactor
    {
        get => (double)GetValue(ScaleFactorProperty);
        set => SetValue(ScaleFactorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="AreaSymbol"/> bindable property.
    /// </summary>
    public static readonly BindableProperty AreaSymbolProperty =
        BindableProperty.Create(nameof(AreaSymbol), typeof(Symbol), typeof(OverviewMap), null, propertyChanged: OnAreaSymbolChanged);

    /// <summary>
    /// Identifies the <see cref="GeoView"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GeoViewProperty =
        BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(OverviewMap), null, BindingMode.OneWay, null, propertyChanged: OnGeoViewPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="Map"/> bindable property.
    /// </summary>
    public static readonly BindableProperty MapProperty =
        BindableProperty.Create(nameof(Map), typeof(Map), typeof(OverviewMap), null, propertyChanged: OnMapPropertyChanged);

    /// <summary>
    /// Identifies the <see cref="PointSymbol"/> bindable property.
    /// </summary>
    public static readonly BindableProperty PointSymbolProperty =
        BindableProperty.Create(nameof(PointSymbol), typeof(Symbol), typeof(OverviewMap), null, propertyChanged: OnPointSymbolChanged);

    /// <summary>
    /// Identifies the <see cref="ScaleFactor"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ScaleFactorProperty =
        BindableProperty.Create(nameof(ScaleFactor), typeof(double), typeof(OverviewMap), 25.0, propertyChanged: OnScaleFactorPropertyChanged);

    private static void OnAreaSymbolChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (((OverviewMap)sender)._controller is OverviewMapController controller)
        {
            controller.AreaSymbol = newValue as Symbol;
        }
    }

    private static void OnGeoViewPropertyChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (((OverviewMap)sender)._controller is OverviewMapController controller)
        {
            controller.AttachedView = newValue as GeoView;
        }
    }

    private static void OnMapPropertyChanged(BindableObject sender, object oldValue, object newValue)
    {
        if ((OverviewMap)sender is OverviewMap sendingView && sendingView._overviewMapView is MapView overview)
        {
            overview.Map = (Map)newValue;
            if (sendingView._controller is OverviewMapController controller && sendingView.GeoView != null)
            {
                controller.ApplyViewpoint(sendingView.GeoView, overview);
            }
        }
    }

    private static void OnPointSymbolChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (((OverviewMap)sender)._controller is OverviewMapController controller)
        {
            controller.PointSymbol = newValue as Symbol;
        }
    }

    private static void OnScaleFactorPropertyChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (((OverviewMap)sender)._controller is OverviewMapController controller)
        {
            controller.ScaleFactor = (double)newValue;
        }
    }
}
