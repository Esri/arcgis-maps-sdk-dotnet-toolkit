#if WPF || MAUI
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

/// <summary>
/// View model for a toolbar representing a category of items. Used for nested views.
/// </summary>
public class OrientedImageryToolbarCategory
{
    /// <summary>The default <see cref="Name"/> of the Mode category.</summary>
    public const string ModeCategoryName = "Mode";
    /// <summary>The default <see cref="Name"/> of the Overlays category.</summary>
    public const string OverlaysCategoryName = "Overlays";
    /// <summary>The default <see cref="Name"/> of the Markers category.</summary>
    public const string MarkersCategoryName = "Markers";
    /// <summary>The default <see cref="Name"/> of the More category.</summary>
    public const string MoreCategoryName = "More";

    private OrientedImageryViewModel? _mainViewModel;
    private HashSet<OrientedImageryToolbarItemBase> _attachedToolbarItems;

    /// <summary>
    /// Constructs an <see cref="OrientedImageryToolbarCategory"/> with the given name.
    /// </summary>
    /// <param name="name"></param>
    public OrientedImageryToolbarCategory(string name)
    {
        Name = name;
        _attachedToolbarItems = new(ReferenceEqualityComparer.Instance);
        ToolbarItems = new ObservableCollection<OrientedImageryToolbarItemBase>();
        ToolbarItems.CollectionChanged += ToolbarItems_CollectionChanged;
    }

    /// <summary>
    /// The category name. This value is used as an identifier for data template selection.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets or sets the linked <see cref="OrientedImageryViewModel"/>. This property will be set automatically when the category item
    /// is added to <see cref="OrientedImageryViewModel.ToolbarCategories"/>.
    /// </summary>
    public OrientedImageryViewModel? ViewModel
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
    /// The collection of toolbar items to be displayed in the <see cref="OrientedImageryView"/> connected to the containing
    /// <see cref="OrientedImageryViewModel"/>.
    /// </summary>
    /// <remarks>
    /// Each toolbar item is an <see cref="OrientedImageryToolbarItemBase"/> object with a reference to the containing category's
    /// <see cref="ViewModel"/>. The list is initially populated with a set of default toolbar items for the base control. Items
    /// are disconnected from the view model when they are removed from the collection.
    /// </remarks>
    public ObservableCollection<OrientedImageryToolbarItemBase> ToolbarItems { get; private set; }

    private void ToolbarItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SynchronizeToolbarItems();

    private void SynchronizeToolbarItems()
    {
        var currentItems = new HashSet<OrientedImageryToolbarItemBase>(ToolbarItems, ReferenceEqualityComparer.Instance);
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
/// Base class for view models backing the toolbar controls of an <see cref="OrientedImageryView"/>.
/// </summary>
/// <remarks>
/// <para>
///   <see cref="OrientedImageryToolbarItemBase"/> objects are meant to be held in <see cref="OrientedImageryToolbarCategory.ToolbarItems"/>.
///   They supply a <see cref="ViewModel"/> property for convenient access to the containing <see cref="OrientedImageryViewModel"/>, and implement
///   <see cref="INotifyPropertyChanged"/>.
/// </para>
/// <para>
///   <see cref="ViewModel"/> is populated when an <see cref="OrientedImageryToolbarItemBase"/> instance is added to <see cref="OrientedImageryToolbarCategory.ToolbarItems"/>.
///   The same <see cref="OrientedImageryToolbarItemBase"/> instance should not be shared between multiple <see cref="OrientedImageryViewModel"/> instances, as only
///   one view model can be referenced by the <see cref="ViewModel"/> property at a time.
/// </para>
/// </remarks>
public abstract class OrientedImageryToolbarItemBase : INotifyPropertyChanged
{
    private OrientedImageryViewModel? _mainViewModel;

    /// <summary>
    /// Gets or sets the linked <see cref="OrientedImageryViewModel"/>. This property will be set automatically when the toolbar item is added to <see cref="OrientedImageryToolbarCategory.ToolbarItems"/>.
    /// </summary>
    public OrientedImageryViewModel? ViewModel
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
    protected virtual void OnViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue) { }

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
/// View model for the "Auto Update Footprint" toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class AutoUpdateFootprintVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Allow Adding Markers" toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class AllowAddingMarkersVM : OrientedImageryToolbarItemBase
{
    /// <summary>
    /// Gets or sets a value indicating whether adding markers is enabled. If set to <see langword="false"/>, the 
    /// view model's interaction state will be set to <see cref="OrientedImageryViewInteractionState.None"/>.
    /// </summary>
    public bool IsAddingMarkersEnabled
    {
        get => ViewModel == null ? false : ViewModel.InteractionState == OrientedImageryViewInteractionState.AddMarkers;
        set
        {
            if (ViewModel == null)
            {
                OnPropertyChanged();
                return;
            }

            if (value)
                ViewModel.InteractionState = OrientedImageryViewInteractionState.AddMarkers;
            else
                ViewModel.InteractionState = OrientedImageryViewInteractionState.None;

            OnPropertyChanged();
        }
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;

        if (newValue != null)
            newValue.PropertyChanged += ViewModel_PropertyChanged;

        OnPropertyChanged(nameof(IsAddingMarkersEnabled));
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrientedImageryViewModel.InteractionState))
            OnPropertyChanged(nameof(IsAddingMarkersEnabled));
    }
}

/// <summary>
/// View model for the image search mode toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class ImageSearchModeVM : OrientedImageryToolbarItemBase
{
    private OrientedImageryViewInteractionState _selectedInteractionState = OrientedImageryViewInteractionState.FindImages;

    /// <summary>
    /// True when <see cref="OrientedImageryViewModel.InteractionState"/> is either <see cref="OrientedImageryViewInteractionState.FindImages"/>
    /// or <see cref="OrientedImageryViewInteractionState.DisplayImage"/>, <see langword="false"/> otherwise. When explicitly set to <see langword="false"/>,
    /// the view model's interaction state will be set to <see cref="OrientedImageryViewInteractionState.None"/>.
    /// </summary>
    public bool IsImageSelectionEnabled
    {
        get
        {
            if (ViewModel == null)
                return false;

            return ViewModel.InteractionState == OrientedImageryViewInteractionState.FindImages ||
                ViewModel.InteractionState == OrientedImageryViewInteractionState.DisplayImage;
        }
        set
        {
            if (ViewModel == null)
                return;
            else if (value)
                ViewModel.InteractionState = _selectedInteractionState;
            else
                ViewModel.InteractionState = OrientedImageryViewInteractionState.None;
        }
    }

    /// <summary>
    /// Gets the available image search modes.
    /// </summary>
    public IReadOnlyList<OrientedImageryViewInteractionState> ImageSearchModes { get; } = new[]
    {
        OrientedImageryViewInteractionState.FindImages,
        OrientedImageryViewInteractionState.DisplayImage
    };

    /// <summary>
    /// Gets or sets the selected image search mode.
    /// </summary>
    public OrientedImageryViewInteractionState SelectedInteractionState
    {
        get => _selectedInteractionState;
        set
        {
            SetProperty(ref _selectedInteractionState, value);
            if (ViewModel is not null)
            {
                ViewModel.InteractionState = SelectedInteractionState;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue)
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
        if (e.PropertyName == nameof(OrientedImageryViewModel.InteractionState))
        {
            if (ViewModel != null && ImageSearchModes.Contains(ViewModel.InteractionState))
                SetProperty(ref _selectedInteractionState, ViewModel.InteractionState, nameof(SelectedInteractionState));
            OnPropertyChanged(nameof(IsImageSelectionEnabled));
        }
    }
}

/// <summary>
/// View model for the "Show Selected Footprint" toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class ShowSelectedFootprintVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Show Unselected Footprints" toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class ShowUnselectedFootprintsVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Show Camera Markers" toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class ShowCameraMarkersVM : OrientedImageryToolbarItemBase
{
    /// <summary>
    /// Toggles camera markers between visible on the GeoView and hidden.
    /// </summary>
    public ICommand ToggleCameraMarkerDisplayMode { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="ShowCameraMarkersVM" /> class.
    /// </summary>
    public ShowCameraMarkersVM()
    {
        ToggleCameraMarkerDisplayMode = new Command(
        execute: () =>
        {
            if (ViewModel == null)
                return;

            ViewModel.ShowCameraLocations = !ViewModel.ShowCameraLocations;
        },
        canExecute: () => true);
    }
}

/// <summary>
/// View model for the "Clear Markers" toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class ClearMarkersVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for a divider between groups of oriented imagery toolbar controls.
/// </summary>
public class ToolbarDividerVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the sequential navigation toolbar control in the <see cref="OrientedImageryView"/>.
/// </summary>
public class SequentialNavigationVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Select New Marker Symbol" toolbar control in the <see cref="OrientedImageryView"/>.
/// This control allows the user to select a marker symbol to be used for new markers in the <see cref="OrientedImageryView"/>.
/// </summary>
public class SelectNewMarkerSymbolVM : OrientedImageryToolbarItemBase
{
    private static readonly MarkerSymbol DefaultSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 10);

    /// <summary>
    /// Gets the collection of marker symbol options that can be used for selecting a new marker symbol.
    /// </summary>
    public ObservableCollection<MarkerSymbol> SymbolOptions { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="SelectNewMarkerSymbolVM"/> class.
    /// </summary>
    public SelectNewMarkerSymbolVM(Collection<MarkerSymbol>? markers = null)
    {
        SymbolOptions = new ObservableCollection<MarkerSymbol>(markers ?? new Collection<MarkerSymbol>());
        SymbolOptions.CollectionChanged += MarkerSymbolOptions_CollectionChanged;

        _selectedSymbol = SymbolOptions.Count > 0 ? SymbolOptions[0] : DefaultSymbol;
    }

    private void MarkerSymbolOptions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

    private MarkerSymbol _selectedSymbol;
    /// <summary>
    /// Gets or sets the currently-selected marker symbol.
    /// </summary>
    public MarkerSymbol SelectedSymbol
    {
        get => _selectedSymbol;
        set
        {
            if (value == _selectedSymbol) { return; }
            SetProperty(ref _selectedSymbol, value);
            if (ViewModel != null)
            {
                ViewModel.NewMarkerSymbol = _selectedSymbol;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue)
    {
        base.OnViewModelChanged(oldValue, newValue);
        if (ViewModel != null)
            ViewModel.NewMarkerSymbol = _selectedSymbol;
    }
}
#endif
