#if WINDOWS_XAML

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageDisplayName, Type = typeof(OrientedImageDisplay))]
public partial class OrientedImageryView : ItemsControl
{
    private long _isBusyCallbackToken;
    private long _isInteractiveCallbackToken;
    private long _errorCallbackToken;

    internal SelectNewMarkerSymbolVM MarkerSymbolPicker { get; private set; } = null!;

    internal ShowCameraMarkersVM CameraMarkers { get; private set; } = null!;

    private void InitializePlatform()
    {
        var toolbarItems = GetDefaultToolbarItems();
        MarkerSymbolPicker = toolbarItems.OfType<SelectNewMarkerSymbolVM>().Single();
        CameraMarkers = toolbarItems.OfType<ShowCameraMarkersVM>().Single();
    }

    private void SetToolbarViewModels(OrientedImageryViewModel? viewModel)
    {
        MarkerSymbolPicker.MainViewModel = viewModel;
        CameraMarkers.MainViewModel = viewModel;
    }

    private void UpdateDisplayStateSubscriptions(OrientedImageDisplay? oldDisplay, OrientedImageDisplay? newDisplay)
    {
        if (oldDisplay != null)
        {
            oldDisplay.UnregisterPropertyChangedCallback(OrientedImageDisplay.IsBusyProperty, _isBusyCallbackToken);
            oldDisplay.UnregisterPropertyChangedCallback(OrientedImageDisplay.IsInteractiveProperty, _isInteractiveCallbackToken);
            oldDisplay.UnregisterPropertyChangedCallback(OrientedImageDisplay.ErrorProperty, _errorCallbackToken);
        }

        if (newDisplay != null)
        {
            _isBusyCallbackToken = newDisplay.RegisterPropertyChangedCallback(OrientedImageDisplay.IsBusyProperty, OnDisplayStateChanged);
            _isInteractiveCallbackToken = newDisplay.RegisterPropertyChangedCallback(OrientedImageDisplay.IsInteractiveProperty, OnDisplayStateChanged);
            _errorCallbackToken = newDisplay.RegisterPropertyChangedCallback(OrientedImageDisplay.ErrorProperty, OnDisplayStateChanged);
        }

        UpdateDisplayState();
    }

    private void OnDisplayStateChanged(DependencyObject sender, DependencyProperty property)
        => UpdateDisplayState();

    private void UpdateDisplayState()
    {
        IsImageDisplayBusy = _display?.IsBusy ?? false;
        IsImageDisplayInteractive = _display?.IsInteractive ?? false;
        ImageDisplayError = _display?.Error;
    }

    internal bool IsImageDisplayBusy
    {
        get => (bool)GetValue(IsImageDisplayBusyProperty);
        private set => SetValue(IsImageDisplayBusyProperty, value);
    }

    internal static readonly DependencyProperty IsImageDisplayBusyProperty =
        DependencyProperty.Register(nameof(IsImageDisplayBusy), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal bool IsImageDisplayInteractive
    {
        get => (bool)GetValue(IsImageDisplayInteractiveProperty);
        private set => SetValue(IsImageDisplayInteractiveProperty, value);
    }

    internal static readonly DependencyProperty IsImageDisplayInteractiveProperty =
        DependencyProperty.Register(nameof(IsImageDisplayInteractive), typeof(bool), typeof(OrientedImageryView), new PropertyMetadata(false));

    internal Exception? ImageDisplayError
    {
        get => (Exception?)GetValue(ImageDisplayErrorProperty);
        private set => SetValue(ImageDisplayErrorProperty, value);
    }

    internal static readonly DependencyProperty ImageDisplayErrorProperty =
        DependencyProperty.Register(nameof(ImageDisplayError), typeof(Exception), typeof(OrientedImageryView), new PropertyMetadata(null));
}

#endif
