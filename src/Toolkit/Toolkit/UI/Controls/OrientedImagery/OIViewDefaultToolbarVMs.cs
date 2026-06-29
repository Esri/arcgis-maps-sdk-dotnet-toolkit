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

public abstract class OrientedImageryViewToolbarVM : INotifyPropertyChanged
{
    private OrientedImageryViewModel? _mainViewModel;

    /// <summary>
    /// Gets or sets the main view model for the oriented imagery view. This property will be set automatically when the toolbar is added to <see cref="OrientedImageryView.ItemsSource"/>.
    /// </summary>
    public OrientedImageryViewModel? MainViewModel
    {
        get => _mainViewModel;
        set
        {
            if (value == _mainViewModel) return;
            var oldValue = _mainViewModel;
            _mainViewModel = value;
            OnMainViewModelChanged(oldValue, value);
        }
    }

    /// <summary>
    /// Called when the <see cref="MainViewModel"/> property changes. Override this method to handle changes to the main view model.
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

public class AutoUpdateFootprintVM : OrientedImageryViewToolbarVM { }

public class ShowSelectedFootprintVM : OrientedImageryViewToolbarVM { }

public class ShowUnselectedFootprintsVM : OrientedImageryViewToolbarVM { }

public class ShowCameraMarkersVM : OrientedImageryViewToolbarVM { }

public class ClearMarkersVM : OrientedImageryViewToolbarVM { }

/// <summary>
/// View model for the "Select New Marker Symbol" toolbar control in the Oriented Imagery View. This control allows the user to loop through a collection of marker symbols and select one to be used for new markers in the oriented imagery view.
/// </summary>
public class SelectNewMarkerSymbolVM : OrientedImageryViewToolbarVM
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
            if (MainViewModel != null)
            {
                MainViewModel.NewMarkerSymbol = _selectedSymbol;
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
        if (MainViewModel != null)
            MainViewModel.NewMarkerSymbol = _selectedSymbol;
    }
}
#endif
