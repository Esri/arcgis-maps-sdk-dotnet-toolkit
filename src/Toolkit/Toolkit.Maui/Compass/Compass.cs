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

using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;
using Geom = Microsoft.Maui.Controls.Shapes.Geometry;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// The Compass Control showing the heading on the map when the rotation is not North up / 0.
/// </summary>
public class Compass : TemplatedView
#pragma warning disable CS0618 // Type or member is obsolete
    , ICompass
#pragma warning restore CS0618 // Type or member is obsolete
{
    private static readonly ControlTemplate DefaultControlTemplate;
    private bool _headingSetByGeoView;
    private bool _isVisible;

    static Compass()
    {
        DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
    }
    
    private static object BuildDefaultTemplate()
    {
        Grid layoutRoot = new Grid();
        for (int i = 0; i < 5; i++)
            layoutRoot.RowDefinitions.Add(new RowDefinition());
        layoutRoot.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
        layoutRoot.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
        layoutRoot.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
        Ellipse ellipse = new Ellipse { StrokeThickness = 1, Fill = new SolidColorBrush(Color.FromRgba(0xFF, 0xFF, 0xFF, 0x55)), Stroke = new SolidColorBrush(Colors.Gray) };
        Grid.SetColumnSpan(ellipse, 3);
        Grid.SetRowSpan(ellipse, 5);
        layoutRoot.Children.Add(ellipse);
        Grid arrowRoot = new Grid();
        Grid.SetRow(arrowRoot, 1);
        Grid.SetColumn(arrowRoot, 1);
        Grid.SetRowSpan(arrowRoot, 3);
        arrowRoot.RowDefinitions.Add(new RowDefinition());
        arrowRoot.RowDefinitions.Add(new RowDefinition());
        arrowRoot.ColumnDefinitions.Add(new ColumnDefinition());
        arrowRoot.ColumnDefinitions.Add(new ColumnDefinition());
        var pathConverter = new PathGeometryConverter();
        Path p1 = new Path() { Aspect = Stretch.Fill, StrokeThickness = 0, Fill = new SolidColorBrush(Colors.Red), Data = (Geom)pathConverter.ConvertFromInvariantString("M0,10 L10,10 10,0 z")! };
        Path p2 = new Path() { Aspect = Stretch.Fill, StrokeThickness = 0, Fill = new SolidColorBrush(Colors.DarkRed), Data = (Geom)pathConverter.ConvertFromInvariantString("M0,0 L0,10 10,10 z")! };
        Path p3 = new Path() { Aspect = Stretch.Fill, StrokeThickness = 0, Fill = new SolidColorBrush(Colors.DarkGray), Data = (Geom)pathConverter.ConvertFromInvariantString("M0,0 L10,10 10,0 z")! };
        Path p4 = new Path() { Aspect = Stretch.Fill, StrokeThickness = 0, Fill = new SolidColorBrush(Colors.Gray), Data = (Geom)pathConverter.ConvertFromInvariantString("M0,0 L0,10 10,0 z")! };
        Grid.SetColumn(p2, 1);
        Grid.SetRow(p3, 1);
        Grid.SetColumn(p4, 1);
        Grid.SetRow(p4, 1);
        arrowRoot.Children.Add(p1);
        arrowRoot.Children.Add(p2);
        arrowRoot.Children.Add(p3);
        arrowRoot.Children.Add(p4);
        layoutRoot.Children.Add(arrowRoot);
        Ellipse center = new Ellipse()
        {
            WidthRequest = 2,
            HeightRequest = 2,
            HorizontalOptions = new LayoutOptions(LayoutAlignment.Center, false),
            VerticalOptions = new LayoutOptions(LayoutAlignment.Center, false),
            Fill = new SolidColorBrush(Colors.Orange)
        };
        Grid.SetColumn(center, 1);
        Grid.SetRow(center, 2);
        layoutRoot.Children.Add(center);

        INameScope nameScope = new NameScope();
        NameScope.SetNameScope(layoutRoot, nameScope);
        nameScope.RegisterName("Root", layoutRoot);
        nameScope.RegisterName("Arrow", arrowRoot);
        return layoutRoot;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Compass"/> class.
    /// </summary>
    public Compass()
    {
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Start;
        WidthRequest = 30;
        HeightRequest = 30;
        ControlTemplate = DefaultControlTemplate;
        var tap = new TapGestureRecognizer();
        GestureRecognizers.Add(tap);
        tap.Tapped += Tap_Tapped;
    }

    private void Tap_Tapped(object? sender, TappedEventArgs e)
    {
        ResetRotation();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _isVisible = false;
        UpdateCompassRotation(false);
    }

    /// <summary>
    /// Identifies the <see cref="Heading"/> bindable property.
    /// </summary>
    public static readonly BindableProperty HeadingProperty =
        BindableProperty.Create(nameof(Heading), typeof(double), typeof(Compass), 0d, BindingMode.OneWay, propertyChanged: (s, oldValue, newValue) => ((Compass)s).OnHeadingPropertyChanged((double)oldValue, (double)newValue));

    /// <summary>
    /// Gets or sets the Heading for the compass.
    /// </summary>
    public double Heading
    {
        get { return (double)GetValue(HeadingProperty); }
        set { SetValue(HeadingProperty, value); }
    }

    private void OnHeadingPropertyChanged(double oldValue, double newValue)
    {
        if (GeoView != null && !_headingSetByGeoView)
        {
            throw new InvalidOperationException("The Heading Property is read-only when the GeoView property has been assigned");
        }

        UpdateCompassRotation(true);
    }

    /// <summary>
    /// Identifies the <see cref="AutoHide"/> bindable property.
    /// </summary>
    public static readonly BindableProperty AutoHideProperty =
        BindableProperty.Create(nameof(AutoHide), typeof(bool), typeof(Compass), true, BindingMode.OneWay, propertyChanged: (s, oldValue, newValue) => ((Compass)s).UpdateCompassRotation(false));

    /// <summary>
    /// Gets or sets a value indicating whether to auto-hide the control when Heading is 0.
    /// </summary>
    public bool AutoHide
    {
        get { return (bool)GetValue(AutoHideProperty); }
        set { SetValue(AutoHideProperty, value); }
    }

    /// <summary>
    /// Gets or sets the GeoView property that can be attached to a Compass control to accurately set the heading, instead of
    /// setting the <see cref="Compass.Heading"/> property directly.
    /// </summary>
    public GeoView? GeoView
    {
        get { return GetValue(GeoViewProperty) as GeoView; }
        set { SetValue(GeoViewProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="GeoView"/> Dependency Property.
    /// </summary>
    public static readonly BindableProperty GeoViewProperty =
        BindableProperty.Create(nameof(Compass.GeoView), typeof(GeoView), typeof(Compass), null, BindingMode.OneWay, propertyChanged: (s, oldValue, newValue) => ((Compass)s).OnGeoViewPropertyChanged(oldValue as GeoView, newValue as GeoView));

    private void OnGeoViewPropertyChanged(GeoView? oldValue, GeoView? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= GeoView_PropertyChanged;
        }
        if (newValue is not null)
        {
            newValue.PropertyChanged += GeoView_PropertyChanged;
        }
        UpdateCompassFromGeoView(newValue);
    }

    private void UpdateCompassRotation(bool useTransitions)
    {
        double heading = Heading;
        if (double.IsNaN(heading))
        {
            heading = 0;
        }

        var transform = GetTemplateChild("Arrow") as VisualElement;
        if (transform != null)
        {
            transform.Rotation = -heading;
        }

        bool autoHide = AutoHide && !DesignTime.IsDesignMode;
        if (Math.Round(heading % 360) == 0 && autoHide)
        {
            if (_isVisible)
            {
                _isVisible = false;
                var root = GetTemplateChild("Root") as VisualElement;
                root?.FadeTo(0, 500);
            }
        }
        else if (!_isVisible)
        {
            _isVisible = true;
            var root = GetTemplateChild("Root") as VisualElement;
            root?.FadeTo(1, 500);
        }
    }

    private void GeoView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapView.MapRotation) || e.PropertyName == nameof(SceneView.Camera) || e.PropertyName == nameof(LocalSceneView.Camera))
        {
            UpdateCompassFromGeoView(sender as GeoView);
        }
    }
    private void UpdateCompassFromGeoView(GeoView? view)
    {
        _headingSetByGeoView = true;
        Heading = (view is MapView mv) ? mv.MapRotation : (view is SceneView sv ? sv.Camera.Heading : (view is LocalSceneView lsv ? lsv.Camera.Heading : 0));
        _headingSetByGeoView = false;
    }

    private void ResetRotation()
    {
        var view = GeoView;
        if (view is MapView mv)
        {
            mv.SetViewpointRotationAsync(0);
        }
        else if (view is SceneView sv)
        {
            var c = sv.Camera;
            sv.SetViewpointCameraAsync(c.RotateTo(0, c.Pitch, c.Roll));
        }
        else if (view is LocalSceneView lsv)
        {
            var c = lsv.Camera;
            lsv.SetViewpointCameraAsync(c.RotateTo(0, c.Pitch, c.Roll));
        }
    }
}
