#if WINDOWS
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
using NativeGrid = Microsoft.UI.Xaml.Controls.Grid;
using NativeThickness = Microsoft.UI.Xaml.Thickness;
using NativeVisibility = Microsoft.UI.Xaml.Visibility;
using NativeView = Microsoft.UI.Xaml.FrameworkElement;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal sealed partial class OrientedImageryControlsViewHandler
{
    #region Toolbar state

    private OrientedImageryToolbarItemBase? _item;
    private bool _updating;
    private readonly List<TextBlock> _captions = [];
    private readonly List<ToolbarComboBox> _toolbarSelectors = [];

    #endregion

    #region Toolbar controls

    private void RefreshToolbar(OrientedImageryView owner, OrientedImageryToolbarItemBase item)
    {
        var model = owner.ViewModel;
        if (_model != model || _item != item)
        {
            Clear();
            _model = model;
            _item = item;
            NativeView? control = item switch
            {
                ImageSearchModeVM mode => ModePicker(owner, mode),
                ToolbarDividerVM => Divider(),
                ShowSelectedFootprintVM => Toggle(owner, "ShowSelectedImageFootprint", "CurrentFootprintStackedLabel",
                    Geometry(OrientedImageryView.CurrentFootprintGeometry),
                    () => model.ShowSelectedFootprint, value => model.ShowSelectedFootprint = value,
                    automationId: "OrientedImageryDesktopCurrentFootprint"),
                ShowUnselectedFootprintsVM => Toggle(owner, "ShowUnselectedImageFootprints", "AdditionalFootprintsStackedLabel",
                    Geometry(OrientedImageryView.OverlayGeometry),
                    () => model.ShowUnselectedFootprints, value => model.ShowUnselectedFootprints = value,
                    () => model.Images.Count > 1, "OrientedImageryDesktopAdditionalFootprints"),
                ShowCameraMarkersVM => Toggle(owner, "ToggleCameraMarkers", "CameraLocationsStackedLabel", CameraLocationsIcon(),
                    () => model.ShowCameraLocations, value => model.ShowCameraLocations = value,
                    () => model.Images.Count > 1, "OrientedImageryDesktopCameraLocations"),
                AllowAddingMarkersVM => Toggle(owner, "MobileAddMarkers", "AddMarkersStackedLabel", Glyph("\uE8ED"),
                    () => owner.IsAddingMarkers, owner.SetAddingMarkers,
                    automationId: "OrientedImageryDesktopAddMarker"),
                AutoUpdateFootprintVM => Toggle(owner, "AutoUpdate", "AutoUpdateToolbarLabel", Glyph(ToolkitIcons.Refresh),
                    () => model.AutoUpdateFootprint, value => model.AutoUpdateFootprint = value),
                SelectNewMarkerSymbolVM picker => SymbolPicker(owner, picker),
                ClearMarkersVM => ActionButton(owner, "ClearMarkersLabel", "ClearMarkersStackedLabel", Glyph("\uEB04"),
                    () => model.ClearMarkersCommand.Execute(null), () => model.ClearMarkersCommand.CanExecute(null)),
                SequentialNavigationVM => SequenceButton(owner),
                _ => null,
            };
            if (control != null)
            {
                control.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                control.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                PlatformView.Children.Add(control);
                ((IView)VirtualView).InvalidateMeasure();
            }
        }
        // Setting native selections raises events; do not treat these updates as user input.
        _updating = true;
        try
        {
            foreach (var update in _updates)
                update();
        }
        finally
        {
            _updating = false;
        }
    }

    private NativeButton ActionButton(OrientedImageryView owner, string key, string labelKey, UIElement icon, Action execute, Func<bool> enabled)
    {
        var button = new NativeButton
        {
            Content = LabeledContent(icon, labelKey), MinWidth = 0, MinHeight = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(4), Margin = new NativeThickness(3, 0, 3, 0),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        };
        Describe(button, key);
        button.Click += (_, _) => { if (!_updating && enabled()) execute(); };
        _updates.Add(() => button.IsEnabled = owner.DesktopHasLayer && enabled());
        return button;
    }

    private ToggleButton Toggle(OrientedImageryView owner, string key, string labelKey, UIElement icon, Func<bool> get, Action<bool> set, Func<bool>? enabled = null, string? automationId = null)
    {
        var toggle = new ToggleButton
        {
            Content = LabeledContent(icon, labelKey),
            MinWidth = 0, MinHeight = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(4), Margin = new NativeThickness(3, 0, 3, 0),
        };
        Describe(toggle, key, automationId);
        toggle.Click += (_, _) => { if (!_updating) set(toggle.IsChecked == true); };
        _updates.Add(() =>
        {
            bool selected = get();
            toggle.IsChecked = selected;
            toggle.IsEnabled = owner.DesktopHasLayer && (enabled?.Invoke() ?? true);
        });
        return toggle;
    }

    #endregion

    #region Mode and symbol selectors

    private sealed class ToolbarComboBox : ComboBox
    {
        private readonly bool _showSymbol;
        private double _captionHeight = 32;

        public ToolbarComboBox(bool showSymbol)
        {
            _showSymbol = showSymbol;
            MinWidth = 0;
            MinHeight = 84;
            Padding = new NativeThickness(0);
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            CornerRadius = new CornerRadius(0);
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            UseSystemFocusVisuals = true;
        }

        public void UpdateCaptionHeight(double textScale)
        {
            _captionHeight = 32 * textScale;
            MinHeight = Math.Max(84, 48 + _captionHeight);
            if (GetTemplateChild("ContentPresenter") is NativeView content)
                content.Margin = new NativeThickness(0, 0, 0, 8 + _captionHeight);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Rearrange native parts rather than copying WinUI's keyboard, focus and state template.
            if (GetTemplateChild("LayoutRoot") is NativeGrid root && root.ColumnDefinitions.Count > 1)
                root.ColumnDefinitions[1].Width = new Microsoft.UI.Xaml.GridLength(0);
            if (GetTemplateChild("Background") is Microsoft.UI.Xaml.Controls.Border background)
                background.MinWidth = 0;
            if (GetTemplateChild("ContentPresenter") is Microsoft.UI.Xaml.Controls.ContentPresenter content)
            {
                content.Visibility = _showSymbol ? NativeVisibility.Visible : NativeVisibility.Collapsed;
                content.Width = content.Height = 32;
                content.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                content.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                content.Margin = new NativeThickness(0, 0, 0, 8 + _captionHeight);
                content.RenderTransform = new ScaleTransform { ScaleX = 0.5, ScaleY = 0.5, CenterX = 16, CenterY = 16 };
            }
            if (GetTemplateChild("DropDownGlyph") is NativeView chevron)
            {
                NativeGrid.SetColumn(chevron, 0);
                chevron.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                chevron.Margin = new NativeThickness(0, 40, 0, 0);
            }
            if (GetTemplateChild("Pill") is NativeView pill)
                pill.Visibility = NativeVisibility.Collapsed;
            if (GetTemplateChild("PopupBorder") is Microsoft.UI.Xaml.Controls.Border border)
            {
                border.MinWidth = 40;
                border.CornerRadius = new CornerRadius(0);
            }
            if (GetTemplateChild("Popup") is Microsoft.UI.Xaml.Controls.Primitives.Popup popup)
            {
                popup.PlacementTarget = this;
                popup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft;
            }
        }
    }

    private ToolbarComboBox CreateToolbarSelector(bool showSymbol)
    {
        var selector = new ToolbarComboBox(showSymbol);
        selector.UpdateCaptionHeight(_uiSettings.TextScaleFactor);
        _toolbarSelectors.Add(selector);
        return selector;
    }

    private NativeGrid ModePicker(OrientedImageryView owner, ImageSearchModeVM mode)
    {
        var host = new NativeGrid
        {
            MinHeight = 84,
            ColumnDefinitions =
            {
                new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) },
                new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(24) },
            },
        };
        var button = new ToggleButton
        {
            MinWidth = 0, MinHeight = 84, Padding = new NativeThickness(4),
            BorderThickness = new NativeThickness(1), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(4), Margin = new NativeThickness(3, 0, 3, 0),
        };
        button.Click += (_, _) =>
        {
            if (!_updating && button.IsEnabled)
                mode.IsImageSelectionEnabled = button.IsChecked == true;
        };
        var dropdown = CreateToolbarSelector(false);
        bool selectionChangedWhileOpen = false;
        dropdown.DropDownOpened += (_, _) => selectionChangedWhileOpen = false;
        NativeGrid.SetColumn(dropdown, 1);
        host.Children.Add(button);
        host.Children.Add(dropdown);
        _cleanup.Add(() => dropdown.IsDropDownOpen = false);
        Describe(dropdown, "ChangeMapInteraction", "OrientedImageryDesktopModeDropdown");
        foreach (var state in mode.ImageSearchModes)
        {
            bool search = state == OrientedImageryViewInteractionState.FindImages;
            string key = search ? "ImageSearchModeFind" : "ImageSearchModeDisplay";
            var content = new StackPanel { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal, Spacing = 8 };
            content.Children.Add(new Viewbox { Width = 24, Height = 24, Child = ImageModeIcon(search) });
            content.Children.Add(new TextBlock { Text = Text(key), VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center });
            var item = new ComboBoxItem
            {
                Content = content, Tag = state, MinHeight = 40, Padding = new NativeThickness(8, 4, 8, 4),
            };
            AutomationProperties.SetName(item, Text(key));
            item.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, e) =>
            {
                if (!_updating && dropdown.IsEnabled && item.IsSelected &&
                    e.GetCurrentPoint(item).Properties.IsLeftButtonPressed)
                {
                    e.Handled = true;
                    dropdown.IsDropDownOpen = false;
                    OrientedImageryView.ToggleImageMode(mode, state);
                }
            }), true);
            dropdown.Items.Add(item);
        }
        dropdown.PreviewKeyDown += (_, e) =>
        {
            // Arrow navigation already activates its selection; Enter only confirms it.
            if (!_updating && dropdown.IsEnabled && dropdown.IsDropDownOpen && !selectionChangedWhileOpen &&
                e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space &&
                FocusManager.GetFocusedElement(dropdown.XamlRoot) is ComboBoxItem
                {
                    IsSelected: true, Tag: OrientedImageryViewInteractionState state,
                })
            {
                e.Handled = true;
                dropdown.IsDropDownOpen = false;
                OrientedImageryView.ToggleImageMode(mode, state);
            }
        };
        dropdown.SelectionChanged += (_, _) =>
        {
            if (!_updating && dropdown.IsEnabled &&
                dropdown.SelectedItem is ComboBoxItem { Tag: OrientedImageryViewInteractionState state })
            {
                selectionChangedWhileOpen = dropdown.IsDropDownOpen;
                OrientedImageryView.ToggleImageMode(mode, state);
            }
        };
        bool? previous = null;
        _updates.Add(() =>
        {
            bool search = mode.SelectedInteractionState == OrientedImageryViewInteractionState.FindImages;
            if (previous != search)
            {
                Describe(button, search ? "ImageSearchModeFind" : "ImageSearchModeDisplay", "OrientedImageryDesktopMode");
                if (button.Content is StackPanel previousContent)
                    foreach (var caption in previousContent.Children.OfType<TextBlock>())
                        _captions.Remove(caption);
                button.Content = LabeledContent(ImageModeIcon(search),
                    search ? "ImageSearchModeFindStackedLabel" : "ImageSearchModeDisplayStackedLabel");
                previous = search;
                ((IView)VirtualView).InvalidateMeasure();
            }
            dropdown.SelectedItem = mode.IsImageSelectionEnabled ? dropdown.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is OrientedImageryViewInteractionState state && state == mode.SelectedInteractionState) : null;
            button.IsEnabled = owner.DesktopHasLayer;
            dropdown.IsEnabled = button.IsEnabled;
            button.IsChecked = mode.IsImageSelectionEnabled;
        });
        System.ComponentModel.PropertyChangedEventHandler changed = (_, e) =>
        {
            if (e.PropertyName is null or "" or nameof(ImageSearchModeVM.SelectedInteractionState) or nameof(ImageSearchModeVM.IsImageSelectionEnabled))
                Refresh();
        };
        mode.PropertyChanged += changed;
        _cleanup.Add(() => mode.PropertyChanged -= changed);
        return host;
    }

    private ToggleButton SequenceButton(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        return Toggle(owner, "SequentialMode", "SequentialMode", Geometry(OrientedImageryView.SequenceGeometry),
            () => model.IsSequentialNavigationEnabled, value => model.IsSequentialNavigationEnabled = value,
            () => model.ToggleSequentialNavigationCommand.CanExecute(null), "OrientedImageryDesktopSequentialMode");
    }

    private ComboBox SymbolPicker(OrientedImageryView owner, SelectNewMarkerSymbolVM picker)
    {
        var combo = CreateToolbarSelector(true);
        Describe(combo, "SelectMarkerSymbol", "OrientedImageryDesktopSelectMarkers");
        _cleanup.Add(() => combo.IsDropDownOpen = false);
        MarkerSymbol[] symbols = [];
        combo.SelectionChanged += (_, _) =>
        {
            if (!_updating && combo.IsEnabled && combo.SelectedIndex >= 0 && combo.SelectedIndex < symbols.Length)
                picker.SelectedSymbol = symbols[combo.SelectedIndex];
        };
        void UpdateSymbols()
        {
            if (symbols.SequenceEqual(picker.SymbolOptions)) return;
            symbols = picker.SymbolOptions.ToArray();
            _generation++;
            combo.Items.Clear();
            foreach (var symbol in symbols)
            {
                var image = new Microsoft.UI.Xaml.Controls.Image { Width = 32, Height = 32 };
                string name = OrientedImageryView.MarkerOptionName(symbol, combo.Items.Count);
                var item = new ComboBoxItem
                {
                    Content = image, Padding = new NativeThickness(4),
                    MinWidth = 40, MinHeight = 40,
                    HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                };
                AutomationProperties.SetName(item, name);
                combo.Items.Add(item);
                _ = RenderSymbolAsync(owner, symbol, image, _generation);
            }
        }
        System.ComponentModel.PropertyChangedEventHandler changed = (_, _) => Refresh();
        System.Collections.Specialized.NotifyCollectionChangedEventHandler collectionChanged = (_, _) => Refresh();
        picker.PropertyChanged += changed;
        picker.SymbolOptions.CollectionChanged += collectionChanged;
        _cleanup.Add(() =>
        {
            picker.PropertyChanged -= changed;
            picker.SymbolOptions.CollectionChanged -= collectionChanged;
        });
        _updates.Add(() =>
        {
            UpdateSymbols();
            combo.SelectedIndex = Array.IndexOf(symbols, picker.SelectedSymbol);
            combo.IsEnabled = owner.DesktopHasLayer;
        });
        return combo;
    }

    private async Task RenderSymbolAsync(OrientedImageryView owner, MarkerSymbol symbol, Microsoft.UI.Xaml.Controls.Image image, int generation)
    {
        try
        {
            var swatch = await symbol.CreateSwatchAsync(96);
            var source = await Esri.ArcGISRuntime.UI.RuntimeImageExtensions.ToImageSourceAsync(swatch);
            if (generation == _generation && VirtualView.Owner == owner)
            {
                image.Source = source;
                Refresh();
            }
        }
        catch (Exception ex)
        {
            if (generation == _generation && VirtualView.Owner == owner) owner.ReportDesktopError(ex);
        }
    }

    #endregion
}
#endif
