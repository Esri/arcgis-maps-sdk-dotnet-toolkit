<<<<<<< HEAD
﻿#if WPF || MAUI
||||||| parent of 91d1aa915 (Initial winui implementation - non-extensible toolbar)
﻿#if WPF
=======
﻿#if WPF || WINDOWS_XAML
>>>>>>> 91d1aa915 (Initial winui implementation - non-extensible toolbar)
using Esri.ArcGISRuntime.Symbology;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.OrientedImagery;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
#endif

/// <summary>
/// View model for a toolbar representing a category of items. Used for nested views.
/// </summary>
public class ToolbarCategory
{
    /// <summary>The default <see cref="Name"/> of the Mode category.</summary>
    public const string ModeCategoryName = "Mode";
    /// <summary>The default <see cref="Name"/> of the Overlays category.</summary>
    public const string OverlaysCategoryName = "Overlays";
    /// <summary>The default <see cref="Name"/> of the Markers category.</summary>
    public const string MarkersCategoryName = "Markers";
    /// <summary>The default <see cref="Name"/> of the More category.</summary>
    public const string MoreCategoryName = "More";

    private OrientedImageryViewerModel? _mainViewModel;
    private HashSet<ToolbarItemBase> _attachedToolbarItems;

    /// <summary>
    /// Constructs an <see cref="ToolbarCategory"/> with the given name.
    /// </summary>
    /// <param name="name"></param>
    public ToolbarCategory(string name)
    {
        Name = name;
        _attachedToolbarItems = new(ReferenceEqualityComparer.Instance);
        ToolbarItems = new ObservableCollection<ToolbarItemBase>();
        ToolbarItems.CollectionChanged += ToolbarItems_CollectionChanged;
    }

    /// <summary>
    /// The category name. This value is used as an identifier for data template selection.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets or sets the linked <see cref="OrientedImageryViewerModel"/>. This property will be set automatically when the category item
    /// is added to <see cref="OrientedImageryViewerModel.ToolbarCategories"/>.
    /// </summary>
    public OrientedImageryViewerModel? ViewModel
    {
        get => _mainViewModel;
        set
        {
            if (value == _mainViewModel) return;
            var oldValue = _mainViewModel;
            _mainViewModel = value;
            SynchronizeToolbarItems();
        }
    }

    /// <summary>
    /// The collection of toolbar items to be displayed in the <see cref="OrientedImageryViewer"/> connected to the containing
    /// <see cref="OrientedImageryViewerModel"/>.
    /// </summary>
    /// <remarks>
    /// Each toolbar item is an <see cref="ToolbarItemBase"/> object with a reference to the containing category's
    /// <see cref="ViewModel"/>. The list is initially populated with a set of default toolbar items for the base control. Items
    /// are disconnected from the view model when they are removed from the collection.
    /// </remarks>
    public ObservableCollection<ToolbarItemBase> ToolbarItems { get; private set; }

    private void ToolbarItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SynchronizeToolbarItems();

    private void SynchronizeToolbarItems()
    {
        var currentItems = new HashSet<ToolbarItemBase>(ToolbarItems, ReferenceEqualityComparer.Instance);
        foreach (var item in _attachedToolbarItems)
        {
            if (!currentItems.Contains(item))
                item.ViewModel = null;
        }

        foreach (var item in ToolbarItems)
        {
            item.ViewModel = _mainViewModel;
        }
        _attachedToolbarItems = currentItems;
    }
}

/// <summary>
/// Base class for view models backing the toolbar controls of an <see cref="OrientedImageryViewer"/>.
/// </summary>
/// <remarks>
/// <para>
///   <see cref="ToolbarItemBase"/> objects are meant to be held in <see cref="ToolbarCategory.ToolbarItems"/>.
///   They supply a <see cref="ViewModel"/> property for convenient access to the containing <see cref="OrientedImageryViewerModel"/>, and implement
///   <see cref="INotifyPropertyChanged"/>.
/// </para>
/// <para>
///   <see cref="ViewModel"/> is populated when an <see cref="ToolbarItemBase"/> instance is added to <see cref="ToolbarCategory.ToolbarItems"/>.
///   The same <see cref="ToolbarItemBase"/> instance should not be shared between multiple <see cref="OrientedImageryViewerModel"/> instances, as only
///   one view model can be referenced by the <see cref="ViewModel"/> property at a time.
/// </para>
/// </remarks>
public abstract class ToolbarItemBase : INotifyPropertyChanged
{
    private OrientedImageryViewerModel? _mainViewModel;

    /// <summary>
    /// Gets or sets the linked <see cref="OrientedImageryViewerModel"/>. This property will be set automatically when the toolbar item is added to <see cref="ToolbarCategory.ToolbarItems"/>.
    /// </summary>
    public OrientedImageryViewerModel? ViewModel
    {
        get => _mainViewModel;
        set
        {
            if (value == _mainViewModel) return;
            var oldValue = _mainViewModel;
            _mainViewModel = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));
            OnViewModelChanged(oldValue, value);
        }
    }

    /// <summary>
    /// Called when the <see cref="ViewModel"/> property changes. Override this method to handle changes to the main view model.
    /// </summary>
    protected virtual void OnViewModelChanged(OrientedImageryViewerModel? oldValue, OrientedImageryViewerModel? newValue) { }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the property and raises the <see cref="PropertyChanged"/> event if the value has changed.
    /// </summary>
    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event wihout setting the value of the property.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for the "Allow Adding Markers" toolbar control in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class AllowAddingMarkersVM : ToolbarItemBase
{
    /// <summary>
    /// Gets or sets a value indicating whether adding markers is enabled. If set to <see langword="false"/>, the 
    /// view model's interaction state will be set to <see cref="OrientedImageryViewerInteractionState.None"/>.
    /// </summary>
    public bool IsAddingMarkersEnabled
    {
        get => ViewModel == null ? false : ViewModel.InteractionState == OrientedImageryViewerInteractionState.AddMarkers;
        set
        {
            if (IsAddingMarkersEnabled == value)
                return;

            if (ViewModel == null)
            {
                OnPropertyChanged();
                return;
            }

            if (value)
                ViewModel.InteractionState = OrientedImageryViewerInteractionState.AddMarkers;
            else
                ViewModel.InteractionState = OrientedImageryViewerInteractionState.None;

            OnPropertyChanged();
        }
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(OrientedImageryViewerModel? oldValue, OrientedImageryViewerModel? newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

        if (newValue != null)
            newValue.PropertyChanged += ViewModel_PropertyChanged;

        OnPropertyChanged(nameof(IsAddingMarkersEnabled));
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrientedImageryViewerModel.InteractionState))
            OnPropertyChanged(nameof(IsAddingMarkersEnabled));
    }
}

/// <summary>
/// View model for the image search mode toolbar control in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class ImageSearchModeVM : ToolbarItemBase
{
    private OrientedImageryViewerInteractionState _selectedInteractionState = OrientedImageryViewerInteractionState.FindImages;

    /// <summary>
    /// True when <see cref="OrientedImageryViewerModel.InteractionState"/> is either <see cref="OrientedImageryViewerInteractionState.FindImages"/>
    /// or <see cref="OrientedImageryViewerInteractionState.DisplayImage"/>, <see langword="false"/> otherwise. When set to <see langword="false"/>,
    /// the view model's interaction state will be set to <see cref="OrientedImageryViewerInteractionState.None"/>.
    /// </summary>
    public bool IsImageSelectionEnabled
    {
        get
        {
            if (ViewModel == null)
                return false;

            return ViewModel.InteractionState == OrientedImageryViewerInteractionState.FindImages ||
                ViewModel.InteractionState == OrientedImageryViewerInteractionState.DisplayImage;
        }
        set
        {
            if (IsImageSelectionEnabled == value || ViewModel == null)
                return;
            else if (value)
                ViewModel.InteractionState = _selectedInteractionState;
            else
                ViewModel.InteractionState = OrientedImageryViewerInteractionState.None;
        }
    }

    /// <summary>
    /// Gets the available image search modes.
    /// </summary>
    public IReadOnlyList<OrientedImageryViewerInteractionState> ImageSearchModes { get; } = new[]
    {
        OrientedImageryViewerInteractionState.FindImages,
        OrientedImageryViewerInteractionState.DisplayImage
    };

    /// <summary>
    /// Gets or sets the selected image search mode. If set to a new value, will update <see cref="OrientedImageryViewerModel.InteractionState"/>.
    /// </summary>
    public OrientedImageryViewerInteractionState SelectedInteractionState
    {
        get => _selectedInteractionState;
        set
        {
            if (_selectedInteractionState == value)
                return;

            SetProperty(ref _selectedInteractionState, value);
            if (ViewModel is not null)
            {
                ViewModel.InteractionState = SelectedInteractionState;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(OrientedImageryViewerModel? oldValue, OrientedImageryViewerModel? newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

        if (newValue != null)
        {
            if (ImageSearchModes.Contains(newValue.InteractionState))
                SetProperty(ref _selectedInteractionState, newValue.InteractionState, nameof(SelectedInteractionState));
            newValue.PropertyChanged += ViewModel_PropertyChanged;
        }

        OnPropertyChanged(nameof(IsImageSelectionEnabled));
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrientedImageryViewerModel.InteractionState))
        {
            if (ViewModel != null && ImageSearchModes.Contains(ViewModel.InteractionState))
                SetProperty(ref _selectedInteractionState, ViewModel.InteractionState, nameof(SelectedInteractionState));
            OnPropertyChanged(nameof(IsImageSelectionEnabled));
        }
    }
}

/// <summary>
/// View model for the "Show Selected Footprint" toolbar control in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class ShowSelectedFootprintVM : ToolbarItemBase
{
    /// <summary>
    /// Whether or not the show selected image button is in a usable state.
    /// </summary>
    public bool Enabled => ViewModel?.SelectedImage != null && ViewModel.IsShowSelectedFootprintEnabled;

    /// <inheritdoc/>
    protected override void OnViewModelChanged(OrientedImageryViewerModel? oldValue, OrientedImageryViewerModel? newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

        if (newValue != null)
            newValue.PropertyChanged += ViewModel_PropertyChanged;

        OnPropertyChanged(nameof(Enabled));
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrientedImageryViewerModel.SelectedImage) || e.PropertyName == nameof(OrientedImageryViewerModel.IsShowSelectedFootprintEnabled))
            OnPropertyChanged(nameof(Enabled));
    }
}

/// <summary>
/// View model for the "Show Unselected Footprints" toolbar control in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class ShowUnselectedFootprintsVM : ToolbarItemBase { }

/// <summary>
/// View model for the "Show Camera Markers" toolbar control in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class ShowCameraMarkersVM : ToolbarItemBase { }

/// <summary>
/// View model for the "Clear Markers" toolbar control in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class ClearMarkersVM : ToolbarItemBase { }

/// <summary>
/// View model for a divider between groups of oriented imagery toolbar controls.
/// </summary>
internal class ToolbarDividerVM : ToolbarItemBase { }

/// <summary>
/// View model for the sequential navigation toolbar control in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class SequentialNavigationVM : ToolbarItemBase
{
    /// <summary>
    /// Whether or not sequential navigation is enabled.
    /// </summary>
    public bool CanEnterSequentialNavigation =>
        ViewModel != null && (ViewModel.IsSequentialNavigationEnabled || (ViewModel.SupportsSequentialNavigation && ViewModel.SelectedImage != null));

    /// <inheritdoc/>
    protected override void OnViewModelChanged(OrientedImageryViewerModel? oldValue, OrientedImageryViewerModel? newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

        if (newValue != null)
            newValue.PropertyChanged += ViewModel_PropertyChanged;

        OnPropertyChanged(nameof(CanEnterSequentialNavigation));
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrientedImageryViewerModel.IsSequentialNavigationEnabled) ||
            e.PropertyName == nameof(OrientedImageryViewerModel.SupportsSequentialNavigation) ||
            e.PropertyName == nameof(OrientedImageryViewerModel.SelectedImage))
        {
            OnPropertyChanged(nameof(CanEnterSequentialNavigation));
        }
    }
}

/// <summary>
/// View model for the "Select New Marker Symbol" toolbar control in the <see cref="OrientedImageryViewer"/>.
/// This control allows the user to select a marker symbol to be used for new markers in the <see cref="OrientedImageryViewer"/>.
/// </summary>
internal class SelectNewMarkerSymbolVM : ToolbarItemBase
{
    private static readonly LabeledSymbol DefaultSymbol = new LabeledSymbol(
        new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 10),
        Properties.Resources.GetString("OrientedImageryViewerMarkerX") ?? "Cross marker");

    /// <summary>
    /// Gets the collection of marker symbol options that can be used for selecting a new marker symbol.
    /// </summary>
    public ObservableCollection<LabeledSymbol> SymbolOptions { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="SelectNewMarkerSymbolVM"/> class.
    /// </summary>
    public SelectNewMarkerSymbolVM(Collection<LabeledSymbol>? markers = null)
    {
        SymbolOptions = new ObservableCollection<LabeledSymbol>(markers ?? new Collection<LabeledSymbol>());
        SymbolOptions.CollectionChanged += MarkerSymbolOptions_CollectionChanged;

        _selectedSymbol = SymbolOptions.Count > 0 ? SymbolOptions[0] : DefaultSymbol;
    }

    private void MarkerSymbolOptions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SymbolOptions.Count < 1)
        {
            SelectedSymbol = DefaultSymbol;
        }
        else if (!SymbolOptions.Contains(SelectedSymbol))
        {
            SelectedSymbol = SymbolOptions[0];
        }
    }

    private LabeledSymbol _selectedSymbol;
    /// <summary>
    /// Gets or sets the currently-selected marker symbol.
    /// </summary>
    public LabeledSymbol SelectedSymbol
    {
        get => _selectedSymbol;
        set
        {
            if (value == _selectedSymbol) { return; }
            SetProperty(ref _selectedSymbol, value);
            if (ViewModel != null)
            {
                ViewModel.NewMarkerSymbol = _selectedSymbol.Symbol;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnViewModelChanged(OrientedImageryViewerModel? oldValue, OrientedImageryViewerModel? newValue)
    {
        base.OnViewModelChanged(oldValue, newValue);
        if (ViewModel != null)
            ViewModel.NewMarkerSymbol = _selectedSymbol.Symbol;
    }
}

/// <summary>
/// A <see cref="Symbology.Symbol"/> and label.
/// </summary>
internal sealed class LabeledSymbol(Symbol Symbol, string Label)
{
    /// <summary>
    /// Gets the symbol definition.
    /// </summary>
    public Symbol Symbol { get; private set; } = Symbol;

    /// <summary>
    /// Gets the symbol label.
    /// </summary>
    public string Label { get; private set; } = Label;
}
#endif
