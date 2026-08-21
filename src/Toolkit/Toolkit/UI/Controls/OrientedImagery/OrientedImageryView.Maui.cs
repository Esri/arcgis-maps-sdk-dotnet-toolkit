#if MAUI
using Esri.ArcGISRuntime.Symbology;
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class OrientedImageryView : TemplatedView
{
    private const string AutoUpdateButtonName = "PART_AutoUpdateButton";
    private const string SelectedFootprintButtonName = "PART_SelectedFootprintButton";
    private const string UnselectedFootprintsButtonName = "PART_UnselectedFootprintsButton";
    private const string CameraMarkersButtonName = "PART_CameraMarkersButton";
    private const string AddMarkersButtonName = "PART_AddMarkersButton";
    private const string MarkerSymbolButtonName = "PART_MarkerSymbolButton";
    private const string ClearMarkersButtonName = "PART_ClearMarkersButton";
    private const string PreviousImageButtonName = "PART_PreviousImageButton";
    private const string NextImageButtonName = "PART_NextImageButton";
    private const string NoImageLabelName = "PART_NoImageLabel";
    private const string ErrorLabelName = "PART_ErrorLabel";

    private static readonly ControlTemplate DefaultControlTemplate = new(BuildDefaultTemplate);

    private Button? _autoUpdateButton;
    private Button? _selectedFootprintButton;
    private Button? _unselectedFootprintsButton;
    private Button? _cameraMarkersButton;
    private Button? _addMarkersButton;
    private Button? _markerSymbolButton;
    private Button? _clearMarkersButton;
    private Button? _previousImageButton;
    private Button? _nextImageButton;
    private Label? _noImageLabel;
    private Label? _errorLabel;
    private int _markerSymbolIndex = 2;

    private static object BuildDefaultTemplate()
    {
        Grid root = new()
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
            },
        };
        INameScope nameScope = new NameScope();
        NameScope.SetNameScope(root, nameScope);

        HorizontalStackLayout toolbar = new()
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 2,
        };
        toolbar.Children.Add(CreateToolbarButton(nameScope, AutoUpdateButtonName, "\uE9C4", "OrientedImageryAutoUpdateFootprint"));
        toolbar.Children.Add(CreateToolbarButton(nameScope, SelectedFootprintButtonName, "\uE8A8", "OrientedImageryShowSelectedFootprint"));
        toolbar.Children.Add(CreateToolbarButton(nameScope, UnselectedFootprintsButtonName, "\uE8A7", "OrientedImageryShowUnselectedFootprints"));
        toolbar.Children.Add(CreateToolbarButton(nameScope, CameraMarkersButtonName, "\uE7C0", "OrientedImageryShowCameraMarkers"));
        toolbar.Children.Add(CreateToolbarButton(nameScope, AddMarkersButtonName, "\uE9F6", "OrientedImageryAllowAddingMarkers"));

        Button markerSymbolButton = CreateToolbarButton(nameScope, MarkerSymbolButtonName, "\u25C6", "OrientedImagerySelectMarkerSymbol");
        markerSymbolButton.FontFamily = null;
        markerSymbolButton.TextColor = Colors.Orange;
        toolbar.Children.Add(markerSymbolButton);
        toolbar.Children.Add(CreateToolbarButton(nameScope, ClearMarkersButtonName, ToolkitIcons.Trash, "OrientedImageryClearMarkers"));

        ScrollView toolbarScroller = new()
        {
            Content = toolbar,
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
        };
        root.Children.Add(toolbarScroller);

        Grid displayGrid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        Grid.SetRow(displayGrid, 1);

        OrientedImageDisplay display = new()
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        nameScope.RegisterName(ImageDisplayName, display);
        Grid.SetColumnSpan(display, 3);
        displayGrid.Children.Add(display);

        ActivityIndicator busyIndicator = new()
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
        };
        busyIndicator.SetBinding(ActivityIndicator.IsRunningProperty, static (OrientedImageDisplay source) => source.IsBusy, source: display);
        busyIndicator.SetBinding(IsVisibleProperty, static (OrientedImageDisplay source) => source.IsBusy, source: display);
        Grid.SetColumnSpan(busyIndicator, 3);
        displayGrid.Children.Add(busyIndicator);

        Label noImageLabel = new()
        {
            Text = Properties.Resources.GetString("OrientedImageryNoImagesSelected"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        nameScope.RegisterName(NoImageLabelName, noImageLabel);
        Grid.SetColumnSpan(noImageLabel, 3);
        displayGrid.Children.Add(noImageLabel);

        Label errorLabel = new()
        {
            BackgroundColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = 8,
            Padding = 8,
        };
        nameScope.RegisterName(ErrorLabelName, errorLabel);
        Grid.SetColumnSpan(errorLabel, 3);
        displayGrid.Children.Add(errorLabel);

        Button previousButton = CreateNavigationButton(nameScope, PreviousImageButtonName, ToolkitIcons.ChevronLeft, "OrientedImageryPreviousImage");
        previousButton.VerticalOptions = LayoutOptions.Center;
        displayGrid.Children.Add(previousButton);

        Button nextButton = CreateNavigationButton(nameScope, NextImageButtonName, ToolkitIcons.ChevronRight, "OrientedImageryNextImage");
        nextButton.VerticalOptions = LayoutOptions.Center;
        Grid.SetColumn(nextButton, 2);
        displayGrid.Children.Add(nextButton);

        root.Children.Add(displayGrid);
        return root;
    }

    private static Button CreateToolbarButton(INameScope nameScope, string name, string glyph, string resourceKey)
    {
        Button button = new()
        {
            Text = glyph,
            FontFamily = ToolkitIcons.FontFamilyName,
            FontSize = 24,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            HeightRequest = 44,
            WidthRequest = 44,
            Padding = 6,
        };
        nameScope.RegisterName(name, button);
        string? description = Properties.Resources.GetString(resourceKey);
        SemanticProperties.SetDescription(button, description);
        ToolTipProperties.SetText(button, description);
        return button;
    }

    private static Button CreateNavigationButton(INameScope nameScope, string name, string glyph, string resourceKey)
    {
        Button button = CreateToolbarButton(nameScope, name, glyph, resourceKey);
        button.BackgroundColor = Color.FromArgb("#80FFFFFF");
        button.WidthRequest = 36;
        return button;
    }

    partial void BeforeApplyMauiTemplate()
    {
        UnwireMauiControls();
        if (_display != null)
            _display.PropertyChanged -= Display_PropertyChanged;
    }

    partial void AfterApplyMauiTemplate()
    {
        _autoUpdateButton = FindButton(AutoUpdateButtonName);
        _selectedFootprintButton = FindButton(SelectedFootprintButtonName);
        _unselectedFootprintsButton = FindButton(UnselectedFootprintsButtonName);
        _cameraMarkersButton = FindButton(CameraMarkersButtonName);
        _addMarkersButton = FindButton(AddMarkersButtonName);
        _markerSymbolButton = FindButton(MarkerSymbolButtonName);
        _clearMarkersButton = FindButton(ClearMarkersButtonName);
        _previousImageButton = FindButton(PreviousImageButtonName);
        _nextImageButton = FindButton(NextImageButtonName);
        _noImageLabel = GetTemplateChild(NoImageLabelName) as Label;
        _errorLabel = GetTemplateChild(ErrorLabelName) as Label;

        if (_autoUpdateButton != null) _autoUpdateButton.Clicked += AutoUpdateButton_Clicked;
        if (_selectedFootprintButton != null) _selectedFootprintButton.Clicked += SelectedFootprintButton_Clicked;
        if (_unselectedFootprintsButton != null) _unselectedFootprintsButton.Clicked += UnselectedFootprintsButton_Clicked;
        if (_cameraMarkersButton != null) _cameraMarkersButton.Clicked += CameraMarkersButton_Clicked;
        if (_addMarkersButton != null) _addMarkersButton.Clicked += AddMarkersButton_Clicked;
        if (_markerSymbolButton != null) _markerSymbolButton.Clicked += MarkerSymbolButton_Clicked;
        if (_display != null) _display.PropertyChanged += Display_PropertyChanged;

        UpdateMauiVisualState();
    }

    private Button? FindButton(string name) => GetTemplateChild(name) as Button;

    private void UnwireMauiControls()
    {
        if (_autoUpdateButton != null) _autoUpdateButton.Clicked -= AutoUpdateButton_Clicked;
        if (_selectedFootprintButton != null) _selectedFootprintButton.Clicked -= SelectedFootprintButton_Clicked;
        if (_unselectedFootprintsButton != null) _unselectedFootprintsButton.Clicked -= UnselectedFootprintsButton_Clicked;
        if (_cameraMarkersButton != null) _cameraMarkersButton.Clicked -= CameraMarkersButton_Clicked;
        if (_addMarkersButton != null) _addMarkersButton.Clicked -= AddMarkersButton_Clicked;
        if (_markerSymbolButton != null) _markerSymbolButton.Clicked -= MarkerSymbolButton_Clicked;
    }

    private void AutoUpdateButton_Clicked(object? sender, EventArgs e) => ViewModel.AutoUpdateFootprint = !ViewModel.AutoUpdateFootprint;
    private void SelectedFootprintButton_Clicked(object? sender, EventArgs e) => ViewModel.ShowSelectedFootprint = !ViewModel.ShowSelectedFootprint;
    private void UnselectedFootprintsButton_Clicked(object? sender, EventArgs e) => ViewModel.ShowUnselectedFootprints = !ViewModel.ShowUnselectedFootprints;
    private void AddMarkersButton_Clicked(object? sender, EventArgs e) => ViewModel.AllowAddingMarkers = !ViewModel.AllowAddingMarkers;

    private void Display_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrientedImageDisplay.Error))
            UpdateMauiVisualState();
    }

    private void CameraMarkersButton_Clicked(object? sender, EventArgs e)
    {
        if (!ViewModel.ShowCameraLocations)
        {
            ViewModel.ShowCameraLocations = true;
            ViewModel.ShowCameraLocationsOnDisplay = false;
        }
        else if (!ViewModel.ShowCameraLocationsOnDisplay)
        {
            ViewModel.ShowCameraLocationsOnDisplay = true;
        }
        else
        {
            ViewModel.ShowCameraLocations = false;
            ViewModel.ShowCameraLocationsOnDisplay = false;
        }
    }

    private void MarkerSymbolButton_Clicked(object? sender, EventArgs e)
    {
        MarkerSymbol[] symbols =
        [
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Purple, 10),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Yellow, 10),
            new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Orange, 10),
        ];
        _markerSymbolIndex = (_markerSymbolIndex + 1) % symbols.Length;
        ViewModel.NewMarkerSymbol = symbols[_markerSymbolIndex];
        if (_markerSymbolButton != null)
            _markerSymbolButton.TextColor = _markerSymbolIndex switch
            {
                0 => Colors.Purple,
                1 => Colors.Gold,
                _ => Colors.Orange,
            };
    }

    private void UpdateMauiVisualState()
    {
        if (_noImageLabel != null)
            _noImageLabel.IsVisible = ViewModel.SelectedImage == null;
        if (_errorLabel != null)
        {
            _errorLabel.Text = _display?.Error?.Message;
            _errorLabel.IsVisible = _display?.Error != null;
        }

        if (_previousImageButton != null)
            _previousImageButton.Command = ViewModel.SelectPreviousImageCommand;
        if (_nextImageButton != null)
            _nextImageButton.Command = ViewModel.SelectNextImageCommand;
        if (_clearMarkersButton != null)
            _clearMarkersButton.Command = ViewModel.ClearMarkersCommand;

        SetSelected(_autoUpdateButton, ViewModel.AutoUpdateFootprint);
        SetSelected(_selectedFootprintButton, ViewModel.ShowSelectedFootprint);
        SetSelected(_unselectedFootprintsButton, ViewModel.ShowUnselectedFootprints);
        SetSelected(_cameraMarkersButton, ViewModel.ShowCameraLocations);
        SetSelected(_addMarkersButton, ViewModel.AllowAddingMarkers);
    }

    private static void SetSelected(Button? button, bool selected)
    {
        if (button != null)
            button.BackgroundColor = selected ? Color.FromArgb("#FFD3D3D3") : Colors.Transparent;
    }
}
#endif
