#if WPF
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Toolkit.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// Base class for view models backing the toolbar controls of an <see cref="OrientedImageryView"/>.
/// </summary>
/// <remarks>
/// <para>
///   <see cref="OrientedImageryToolbarItemBase"/> objects are meant to be held in <see cref="OrientedImageryViewModel.ToolbarItems"/>.
///   They supply an <see cref="ViewModel"/> property for convenient access to the containing <see cref="OrientedImageryViewModel"/>, and implement
///   <see cref="INotifyPropertyChanged"/>.
/// </para>
/// <para>
///   <see cref="ViewModel"/> is populated when an <see cref="OrientedImageryToolbarItemBase"/> instance is added to <see cref="OrientedImageryViewModel.ToolbarItems"/>.
///   The same <see cref="OrientedImageryToolbarItemBase"/> instance should not be shared between multiple <see cref="OrientedImageryViewModel"/> instances, as only
///   one view model can be referenced by the <see cref="ViewModel"/> property at a time.
/// </para>
/// </remarks>
public abstract class OrientedImageryToolbarItemBase : INotifyPropertyChanged
{
    private OrientedImageryViewModel? _mainViewModel;

    /// <summary>
    /// Gets or sets the linked <see cref="OrientedImageryViewModel"/>. This property will be set automatically when the toolbar item is added to <see cref="OrientedImageryViewModel.ToolbarItems"/>.
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
            OnMainViewModelChanged(oldValue, value);
        }
    }

    /// <summary>
    /// Called when the <see cref="ViewModel"/> property changes. Override this method to handle changes to the main view model.
    /// </summary>
    protected virtual void OnMainViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue) { }

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
}

/// <summary>
/// View model for the "Auto Update Footprint" toolbar control in the Oriented Imagery View.
/// </summary>
public class AutoUpdateFootprintVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Allow Adding Markers" toolbar control in the Oriented Imagery View.
/// </summary>
public class AllowAddingMarkersVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Show Selected Footprint" toolbar control in the Oriented Imagery View.
/// </summary>
public class ShowSelectedFootprintVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Show Unselected Footprints" toolbar control in the Oriented Imagery View.
/// </summary>
public class ShowUnselectedFootprintsVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Show Camera Markers" toolbar control in the Oriented Imagery View.
/// </summary>
public class ShowCameraMarkersVM : OrientedImageryToolbarItemBase
{
    private CameraMarkerDisplayMode DisplayMode
    {
        get
        {
            if (ViewModel == null || !ViewModel.ShowCameraLocations)
                return CameraMarkerDisplayMode.Off;

            return ViewModel.ShowCameraLocationsOnDisplay ? CameraMarkerDisplayMode.All : CameraMarkerDisplayMode.GeoView;
        }
        set
        {
            if (ViewModel == null)
                return;

            switch (value)
            {
                case CameraMarkerDisplayMode.Off:
                    ViewModel.ShowCameraLocations = false;
                    ViewModel.ShowCameraLocationsOnDisplay = false;
                    break;
                case CameraMarkerDisplayMode.GeoView:
                    ViewModel.ShowCameraLocations = true;
                    ViewModel.ShowCameraLocationsOnDisplay = false;
                    break;
                case CameraMarkerDisplayMode.All:
                    ViewModel.ShowCameraLocations = true;
                    ViewModel.ShowCameraLocationsOnDisplay = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Advances to the next camera marker display mode.
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

            DisplayMode = DisplayMode switch
            {
                CameraMarkerDisplayMode.Off => CameraMarkerDisplayMode.GeoView,
                CameraMarkerDisplayMode.GeoView => CameraMarkerDisplayMode.All,
                _ => CameraMarkerDisplayMode.Off,
            };
        },
        canExecute: () => true);
    }

    private enum CameraMarkerDisplayMode
    {
        Off,
        GeoView,
        All,
    }
}

/// <summary>
/// View model for the "Clear Markers" toolbar control in the Oriented Imagery View.
/// </summary>
public class ClearMarkersVM : OrientedImageryToolbarItemBase { }

/// <summary>
/// View model for the "Select New Marker Symbol" toolbar control in the Oriented Imagery View.
/// This control allows the user to loop through a collection of marker symbols and select one to be used for new markers in the oriented imagery view.
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

        SelectNextSymbol = new Command(
        execute: () =>
        {
            if (SymbolOptions.Count < 1)
            {
                SelectedSymbol = DefaultSymbol;
            }
            var currentIndex = SymbolOptions.IndexOf(SelectedSymbol);
            if (currentIndex < 0 || currentIndex >= SymbolOptions.Count - 1)
            {
                SelectedSymbol = SymbolOptions[0];
            }
            else
            {
                SelectedSymbol = SymbolOptions[currentIndex + 1];
            }
        },
        canExecute: () => true);
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
    /// Gets the currently-selected marker symbol.
    /// </summary>
    public MarkerSymbol SelectedSymbol
    {
        get => _selectedSymbol;
        private set
        {
            if (value == _selectedSymbol) { return; }
            SetProperty(ref _selectedSymbol, value);
            if (ViewModel != null)
            {
                ViewModel.NewMarkerSymbol = _selectedSymbol;
            }
        }
    }

    /// <summary>
    /// Selects the next symbol from <see cref="SymbolOptions"/>.
    /// </summary>
    /// <remarks>
    /// If the current <see cref="SelectedSymbol"/> is at the end of the <see cref="SymbolOptions"/> collection, this command will wrap around and select the first symbol in the collection.
    /// </remarks>
    public ICommand SelectNextSymbol { get; private set; }

    /// <inheritdoc />
    protected override void OnMainViewModelChanged(OrientedImageryViewModel? oldValue, OrientedImageryViewModel? newValue)
    {
        base.OnMainViewModelChanged(oldValue, newValue);
        if (ViewModel != null)
            ViewModel.NewMarkerSymbol = _selectedSymbol;
    }
}
#endif
