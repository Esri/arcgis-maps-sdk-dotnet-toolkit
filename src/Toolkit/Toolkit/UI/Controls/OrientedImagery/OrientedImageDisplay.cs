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
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;

// The host element that presents the active inner viewer differs per platform.
#if WPF
using ViewerHostElement = System.Windows.Controls.ContentPresenter;
#elif WINDOWS_XAML
using ViewerHostElement = Microsoft.UI.Xaml.Controls.ContentPresenter;
#elif MAUI
using ViewerHostElement = Microsoft.Maui.Controls.ContentView;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// A control that displays an oriented image and allows interaction with it.
/// </summary>
/// <remarks>
/// <para>
/// The control presents one of several inner viewers chosen by the <see cref="Mapping.OrientedImageType"/> of the
/// image referenced by the assigned <see cref="Footprint"/>. This release implements the planar viewer (a map view
/// hosting the image as a raster layer); panoramic/360 and video viewers are not yet available.
/// </para>
/// </remarks>
public partial class OrientedImageDisplay
{
    private const string ViewerHostName = "PART_ViewerHost";

    private ViewerHostElement? _viewerHost;
    private OrientedImagePlanarViewer? _planarViewer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageDisplay"/> class.
    /// </summary>
    public OrientedImageDisplay()
        : base()
    {
#if MAUI
        // MAUI layout containers are not tab stops by default, so no IsTabStop is needed here.
        ControlTemplate = DefaultControlTemplate;
#else
        DefaultStyleKey = typeof(OrientedImageDisplay);
#endif

        // Default localized screen-reader label for the control (consumers may override AutomationProperties.Name).
        string automationName = Properties.Resources.GetString("OrientedImageDisplayAutomationName") ?? "Oriented image viewer";
#if WPF
        System.Windows.Automation.AutomationProperties.SetName(this, automationName);
#elif WINDOWS_XAML
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(this, automationName);
#elif MAUI
        Microsoft.Maui.Controls.SemanticProperties.SetDescription(this, automationName);
#endif
    }

    /// <summary>
    /// Occurs when the user taps the oriented image display.
    /// </summary>
    public event EventHandler<ImageClickedEventArgs>? ImageClicked;

    /// <summary>
    /// Gets or sets the footprint of the oriented image to display.
    /// </summary>
    /// <value>The footprint whose <see cref="OrientedImageFootprint.OrientedImage"/> is shown by the control.</value>
    public OrientedImageFootprint? Footprint
    {
        get => GetValue(FootprintProperty) as OrientedImageFootprint;
        set => SetValue(FootprintProperty, value);
    }

    /// <summary>
    /// Gets or sets the marker graphics to render on top of the oriented image.
    /// </summary>
    /// <value>A collection of graphics drawn over the image, or <c>null</c>.</value>
    public ObservableCollection<Graphic>? Markers
    {
        get => GetValue(MarkersProperty) as ObservableCollection<Graphic>;
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the displayed footprint is automatically recomputed when the
    /// viewport changes.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the control recomputes the visible image corners as the viewer is panned or zoomed and
    /// calls <see cref="OrientedImageFootprint.UpdateAsync(OrientedImagePixelCorners, System.Threading.CancellationToken)"/>
    /// so the footprint rendered on the map stays in sync. The footprint itself is not drawn by this control.
    /// </remarks>
    /// <value>A value indicating whether the footprint is automatically updated. The default is <c>false</c>.</value>
    public bool AutoUpdateFootprint
    {
        get => (bool)GetValue(AutoUpdateFootprintProperty);
        set => SetValue(AutoUpdateFootprintProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Footprint"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FootprintProperty =
        PropertyHelper.CreateProperty<OrientedImageFootprint, OrientedImageDisplay>(nameof(Footprint), null, (s, oldValue, newValue) => s.UpdateViewer());

    /// <summary>
    /// Identifies the <see cref="Markers"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkersProperty =
        PropertyHelper.CreateProperty<ObservableCollection<Graphic>, OrientedImageDisplay>(nameof(Markers), null, (s, oldValue, newValue) => s._planarViewer?.SetMarkers(newValue));

    /// <summary>
    /// Identifies the <see cref="AutoUpdateFootprint"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoUpdateFootprintProperty =
        PropertyHelper.CreateProperty<bool, OrientedImageDisplay>(nameof(AutoUpdateFootprint), false, (s, oldValue, newValue) => s._planarViewer?.SetAutoUpdateFootprint(newValue));

    /// <inheritdoc/>
#if WINDOWS_XAML || MAUI
    protected override void OnApplyTemplate()
#elif WPF
    public override void OnApplyTemplate()
#endif
    {
        base.OnApplyTemplate();
        _viewerHost = GetTemplateChild(ViewerHostName) as ViewerHostElement;
        UpdateViewer();
    }

    /// <summary>
    /// Raises the <see cref="ImageClicked"/> event. Called by the active inner viewer.
    /// </summary>
    /// <param name="location">The tapped location in the viewer's map coordinates.</param>
    internal void OnImageClicked(MapPoint location) => ImageClicked?.Invoke(this, new ImageClickedEventArgs(location));

    /// <summary>
    /// Selects the inner viewer appropriate for the current image type and pushes the current state into it.
    /// </summary>
    private void UpdateViewer()
    {
        if (_viewerHost is null)
        {
            return; // Template not applied yet; OnApplyTemplate will call again.
        }

        OrientedImageType? type = Footprint?.OrientedImage?.OrientedImageType;
        if (type is null || IsPlanar(type.Value))
        {
            OrientedImagePlanarViewer viewer = _planarViewer ??= new OrientedImagePlanarViewer(this);
            if (!ReferenceEquals(_viewerHost.Content, viewer))
            {
                _viewerHost.Content = viewer;
            }

            viewer.SetFootprint(Footprint);
            viewer.SetMarkers(Markers);
            viewer.SetAutoUpdateFootprint(AutoUpdateFootprint);
        }
        else
        {
            // Panoramic/360 and video viewers are not implemented yet.
            _viewerHost.Content = null;
        }
    }

    /// <summary>
    /// Determines whether an image type is presented by the planar viewer (everything that is not a panoramic or video type).
    /// </summary>
    private static bool IsPlanar(OrientedImageType type) => type switch
    {
        OrientedImageType.Panoramic => false,
        OrientedImageType.Aerial360Video => false,
        OrientedImageType.AerialFrameVideo => false,
        OrientedImageType.Terrestrial360Video => false,
        OrientedImageType.TerrestrialFrameVideo => false,
        _ => true,
    };

    /// <summary>
    /// Event arguments for the <see cref="OrientedImageDisplay.ImageClicked"/> event.
    /// </summary>
    public class ImageClickedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageClickedEventArgs"/> class with the specified location.
        /// </summary>
        /// <param name="location">The location of the clicked position in map coordinates.</param>
        public ImageClickedEventArgs(MapPoint location)
        {
            Location = location;
        }

        /// <summary>
        /// Gets the location of the clicked position in map coordinates.
        /// </summary>
        /// <value>The clicked location.</value>
        public MapPoint Location { get; }
    }
}
