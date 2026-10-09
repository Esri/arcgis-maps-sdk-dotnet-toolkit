#if WINDOWS_XAML

using Esri.ArcGISRuntime.Symbology;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageViewportName, Type = typeof(OrientedImageViewport))]
[TemplatePart(Name = ToolbarContainerName, Type = typeof(ItemsControl))]
public partial class OrientedImageryView : ItemsControl
{
    private long _isBusyCallbackToken;
    private long _isInteractiveCallbackToken;
    private long _errorCallbackToken;

    internal SelectNewMarkerSymbolVM MarkerSymbolPicker { get; private set; } = null!;

    internal ShowCameraMarkersVM CameraMarkers { get; private set; } = null!;

    internal AllowAddingMarkersVM AllowAddingMarkers { get; private set; } = null!;

    private void UnwireViewport_WinUI(OrientedImageViewport? viewport)
    {
        if (viewport != null)
        {
            viewport.UnregisterPropertyChangedCallback(OrientedImageViewport.IsBusyProperty, _isBusyCallbackToken);
            viewport.UnregisterPropertyChangedCallback(OrientedImageViewport.IsInteractiveProperty, _isInteractiveCallbackToken);
            viewport.UnregisterPropertyChangedCallback(OrientedImageViewport.ErrorProperty, _errorCallbackToken);
        }

        UpdateViewportState();
    }

    private void WireViewport_WinUI(OrientedImageViewport? viewport)
    {
        if (viewport != null)
        {
            _isBusyCallbackToken = viewport.RegisterPropertyChangedCallback(OrientedImageViewport.IsBusyProperty, OnViewportStateChanged);
            _isInteractiveCallbackToken = viewport.RegisterPropertyChangedCallback(OrientedImageViewport.IsInteractiveProperty, OnViewportStateChanged);
            _errorCallbackToken = viewport.RegisterPropertyChangedCallback(OrientedImageViewport.ErrorProperty, OnViewportStateChanged);
        }

        UpdateViewportState();
    }

    private void OnViewportStateChanged(DependencyObject sender, DependencyProperty property)
        => UpdateViewportState();

    private void UpdateViewportState()
    {
        IsImageViewportBusy = _viewport?.IsBusy ?? false;
        IsImageViewportInteractive = _viewport?.IsInteractive ?? false;
        ImageViewportError = _viewport?.Error;
    }

    internal bool IsImageViewportBusy
    {
        get => (bool)GetValue(IsImageViewportBusyProperty);
        private set => SetValue(IsImageViewportBusyProperty, value);
    }

    internal static readonly DependencyProperty IsImageViewportBusyProperty =
        DependencyProperty.Register(nameof(IsImageViewportBusy), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal bool IsImageViewportInteractive
    {
        get => (bool)GetValue(IsImageViewportInteractiveProperty);
        private set => SetValue(IsImageViewportInteractiveProperty, value);
    }

    internal static readonly DependencyProperty IsImageViewportInteractiveProperty =
        DependencyProperty.Register(nameof(IsImageViewportInteractive), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal Exception? ImageViewportError
    {
        get => (Exception?)GetValue(ImageViewportErrorProperty);
        private set => SetValue(ImageViewportErrorProperty, value);
    }

    internal static readonly DependencyProperty ImageViewportErrorProperty =
        DependencyProperty.Register(nameof(ImageViewportError), typeof(Exception), typeof(OrientedImageryView), new PropertyMetadata(null));
}

#endif
