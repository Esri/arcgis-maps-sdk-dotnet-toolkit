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

using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;

#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using Point = Microsoft.Maui.Graphics.Point;
#else
using Esri.ArcGISRuntime.Toolkit.Primitives;
#endif
#if WPF
using Point = System.Windows.Point;
#elif WINDOWS_XAML
using Point = Windows.Foundation.Point;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

public partial class OrientedImageView
{
    private readonly GraphicsOverlay _markerOverlay = new GraphicsOverlay();

    /// <summary>
    /// Initializes a new instance of the <see cref="PopupViewer"/> class.
    /// </summary>
    public OrientedImageView()
        : base()
    {
#if MAUI
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageView);
#endif
        Markers = new OrientedImageMarkerCollection();
        Markers.CollectionChanged += Markers_CollectionChanged;
    }

    /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
    protected override void OnApplyTemplate()
#elif WPF
    public override void OnApplyTemplate()
#endif
    {
        base.OnApplyTemplate();

        LoadImage();
    }

    /// <summary>
    /// Gets or sets the AttachmentsPopupElement.
    /// </summary>
    public OrientedImage? Image
    {
        get { return GetValue(ImageProperty) as OrientedImage; }
        set { SetValue(ImageProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Image"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ImageProperty =
        PropertyHelper.CreateProperty<OrientedImage, OrientedImageView>(nameof(Image), null, (s, oldValue, newValue) => s.LoadImage());

    private void LoadImage()
    {
        if (GetTemplateChild(ImageViewName) is not Image image)
        {
            return;
        }
        if (Image is null)
        {
            image.Source = null;
        }
        else
        {
            // TODO: Load Image
        }
        UpdateMarkers();
    }

    /// <summary>
    /// Gets the collection of markers to be displayed on the image.
    /// </summary>
#if MAUI
    public OrientedImageMarkerCollection Markers { get; private set; }
#elif WPF
    public OrientedImageMarkerCollection Markers
    {
        get { return (OrientedImageMarkerCollection)GetValue(MarkersPropertyKey.DependencyProperty); }
        private set { SetValue(MarkersPropertyKey, value); }
    }

    private static readonly DependencyPropertyKey MarkersPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Markers), typeof(IList<OrientedImageMarker>), typeof(OrientedImageView), null);

    public static readonly DependencyProperty MarkersProperty = MarkersPropertyKey.DependencyProperty;

#elif WINDOWS_XAML
    public OrientedImageMarkerCollection Markers
    {
        get => (OrientedImageMarkerCollection)GetValue(MarkersProperty);
        internal set => SetValue(MarkersProperty, value);
    }
    
    internal static readonly DependencyProperty MarkersProperty = 
        DependencyProperty.Register(nameof(Markers), typeof(OrientedImageMarkerCollection), typeof(OrientedImageView), null);
#endif

    public GeoView GeoView
    {
        get => (GeoView)GetValue(GeoViewProperty);
        set => SetValue(GeoViewProperty, value);
    }

    public static readonly DependencyProperty GeoViewProperty =
        PropertyHelper.CreateProperty<GeoView, OrientedImageView>(nameof(GeoView), null, (s, oldValue, newValue) => s.LoadGeoView(oldValue as GeoView, newValue as GeoView));

    private void LoadGeoView(GeoView? oldView, GeoView? newView)
    {
        oldView?.GraphicsOverlays?.Remove(_markerOverlay);
        if (newView != null)
            newView?.GraphicsOverlays?.Add(_markerOverlay);
    }

    private void Markers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            _markerOverlay.Graphics.Clear();
            foreach (var item in Markers)
            {
                var marker = new Graphic(item.Location, new Symbology.SimpleMarkerSymbol(item.Style, item.Color, item.Size));
                marker.Attributes["Id"] = item.Id;
                _markerOverlay.Graphics.Add(marker);
            }
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
            return;
        else
        {
            if (e.OldItems != null)
            {
                foreach (OrientedImageMarker oldMarker in e.OldItems)
                {
                    var markerToRemove = _markerOverlay.Graphics.FirstOrDefault(g => g.Attributes["Id"]?.Equals(oldMarker.Id) == true);
                    if (markerToRemove != null)
                    {
                        _markerOverlay.Graphics.Remove(markerToRemove);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (OrientedImageMarker newMarker in e.NewItems)
                {
                    var marker = new Graphic(newMarker.Location, new Symbology.SimpleMarkerSymbol(newMarker.Style, newMarker.Color, newMarker.Size));
                    marker.Attributes["Id"] = newMarker.Id;
                    _markerOverlay.Graphics.Add(marker);
                }
            }
        }
        UpdateMarkers();
    }

    private void UpdateMarkers()
    {
        // TODO: Update markers on rendered image
    }
}

public class OrientedImageMarker
{
    internal Guid Id { get; } = Guid.NewGuid();
    public Geometry.MapPoint Location { get; set; }
    public System.Drawing.Color Color { get; set; }
    public Symbology.SimpleMarkerSymbolStyle Style { get; set; } = Symbology.SimpleMarkerSymbolStyle.Circle;
    public double Size { get; set; } = 15d;
}

public class OrientedImageMarkerCollection : ObservableCollection<OrientedImageMarker> { }

public class OrientedImage // TODO: Replace with maps sdk type
{
    public string Filename { get; set; } = string.Empty;
    public Geometry.MapPoint ImageToLocation(Point pixelLocation) => throw new NotImplementedException();
    public Point LocationToImage(Geometry.MapPoint location) => throw new NotImplementedException();
}