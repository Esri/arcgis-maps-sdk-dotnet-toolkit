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
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

/// <summary>
/// Displays a collection of images representing basemaps from ArcGIS Online, a user-defined Portal, or a user-defined collection.
/// </summary>
/// <remarks>
/// If connected to a GeoView, changing the basemap selection will change the connected Map or Scene's basemap.
/// Only basemaps whose spatial reference matches the map or scene's spatial reference can be selected for display.
/// </remarks>
public partial class BasemapGallery
{
    private CollectionView? _listView;
    private readonly BasemapGalleryController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasemapGallery"/> class.
    /// </summary>
    public BasemapGallery()
    {
        _controller = new BasemapGalleryController();
        _controller.PropertyChanged += HandleControllerPropertyChanged;
        ListItemTemplate = DefaultListDataTemplate;
        GridItemTemplate = DefaultGridDataTemplate;
        ControlTemplate = DefaultControlTemplate;
        Loaded += BasemapGallery_Loaded;
    }

    private async void BasemapGallery_Loaded(object? sender, EventArgs e)
    {
        // Unsubscribe from the Loaded event to ensure this only runs once.
        Loaded -= BasemapGallery_Loaded;

        try
        {
            if (AvailableBasemaps is null)
            {
                await _controller.UpdateBasemaps();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Failed to load basemaps: {ex.Message}", "ArcGIS Maps SDK Toolkit");
        }
    }

    private void HandleControllerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BasemapGalleryController.AvailableBasemaps):
                AvailableBasemaps = _controller.AvailableBasemaps;
                break;
            case nameof(BasemapGalleryController.IsLoading):
                _loadingScrim?.SetValue(View.IsVisibleProperty, _controller.IsLoading);
                break;
            case nameof(BasemapGalleryController.SelectedBasemap):
                SelectedBasemap = _controller.SelectedBasemap;
                _listView?.SetValue(CollectionView.SelectedItemProperty, _controller.SelectedBasemap);
                if (_controller.SelectedBasemap != null)
                {
                    BasemapSelected?.Invoke(this, _controller.SelectedBasemap);
                }

                break;
        }
    }

    private CollectionView? ListView
    {
        get => _listView;
        set
        {
            if (value != _listView)
            {
                if (_listView != null)
                {
                    _listView.SelectionChanged -= ListViewSelectionChanged;
                }

                _listView = value;

                if (_listView != null)
                {
                    _listView.SelectionChanged += ListViewSelectionChanged;
                    HandleTemplateChange(Width);
                }
            }
        }
    }

    private static void SelectedBasemapChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (sender is BasemapGallery gallery)
        {
            gallery._controller.SelectedBasemap = newValue as BasemapGalleryItem;
        }
    }

    /// <summary>
    /// Handles property changes for the <see cref="GeoModel" /> bindable property.
    /// </summary>
    private static void GeoModelChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (sender is BasemapGallery gallery)
        {
            gallery._controller.GeoModel = newValue as GeoModel;
        }
    }

    private void ListViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ListView == null)
        {
            return;
        }

        if (e.CurrentSelection.Count == 0)
        {
            SelectedBasemap = null;
        }
        else if (e.CurrentSelection.FirstOrDefault() is BasemapGalleryItem selectedItem)
        {
            if (selectedItem.IsValid)
            {
                SelectedBasemap = selectedItem;
            }
        }
    }

    /// <summary>
    /// Handles property changes for the <see cref="Portal"/> bindable property.
    /// </summary>
    private static void PortalChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (sender is BasemapGallery gallery)
        {
            gallery._controller.Portal = newValue as ArcGISPortal;
        }
    }

    private static void AvailableBasemapsChanged(BindableObject sender, object oldValue, object newValue)
    {
        if (sender is BasemapGallery gallery)
        {
            gallery.ListView?.SetValue(CollectionView.ItemsSourceProperty, newValue);
            if (newValue != gallery._controller.AvailableBasemaps)
            {
                gallery._controller.AvailableBasemaps = newValue as IList<BasemapGalleryItem>;
            }
        }
    }

    /// <summary>
    /// Event raised when a basemap is selected.
    /// </summary>
    public event EventHandler<BasemapGalleryItem>? BasemapSelected;

    #region Convenience Properties

    /// <summary>
    /// Gets or sets the portal used to populate the basemap list.
    /// </summary>
    public ArcGISPortal? Portal
    {
        get => GetValue(PortalProperty) as ArcGISPortal;
        set => SetValue(PortalProperty, value);
    }

    /// <summary>
    /// Gets or sets the connected GeoModel.
    /// </summary>
    public GeoModel? GeoModel
    {
        get => GetValue(GeoModelProperty) as GeoModel;
        set => SetValue(GeoModelProperty, value);
    }

    /// <summary>
    /// Gets or sets the gallery of basemaps to show.
    /// </summary>
    /// <remarks>
    /// When <see cref="Portal"/> is set, this collection will be overwritten.
    /// </remarks>
    public IList<BasemapGalleryItem>? AvailableBasemaps
    {
        get => GetValue(AvailableBasemapsProperty) as IList<BasemapGalleryItem>;
        set => SetValue(AvailableBasemapsProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected basemap.
    /// </summary>
    public BasemapGalleryItem? SelectedBasemap
    {
        get => GetValue(SelectedBasemapProperty) as BasemapGalleryItem;
        set => SetValue(SelectedBasemapProperty, value);
    }

    #endregion ConvenienceProperties

    #region Bindable Properties

    /// <summary>
    /// Identifies the <see cref="Portal"/> bindable property.
    /// </summary>
    public static readonly BindableProperty PortalProperty =
        BindableProperty.Create(nameof(Portal), typeof(ArcGISPortal), typeof(BasemapGallery), null, propertyChanged: PortalChanged);

    /// <summary>
    /// Identifies the <see cref="GeoModel"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GeoModelProperty =
        BindableProperty.Create(nameof(GeoModel), typeof(GeoModel), typeof(BasemapGallery), null, BindingMode.OneWay, null, propertyChanged: GeoModelChanged);

    /// <summary>
    /// Identifies the <see cref="AvailableBasemaps"/> bindable property.
    /// </summary>
    public static readonly BindableProperty AvailableBasemapsProperty =
        BindableProperty.Create(nameof(AvailableBasemaps), typeof(IList<BasemapGalleryItem>), typeof(BasemapGallery), null, BindingMode.OneWay, propertyChanged: AvailableBasemapsChanged);

    /// <summary>
    /// Identifies the <see cref="SelectedBasemap"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SelectedBasemapProperty =
        BindableProperty.Create(nameof(SelectedBasemap), typeof(BasemapGalleryItem), typeof(BasemapGallery), null, BindingMode.OneWay, propertyChanged: SelectedBasemapChanged);

    #endregion Bindable Properties

}
