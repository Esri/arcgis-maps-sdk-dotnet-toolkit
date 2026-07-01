#if WPF // Limiting this to WPF for now to keep things simple

using System.Collections;
using System.Collections.Specialized;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageDisplayName, Type = typeof(OrientedImageDisplay))]
public partial class OrientedImageryView : ItemsControl
{
    private void SetToolbarViewModels(OrientedImageryViewModel? viewModel)
    {
        foreach (var toolbarVM in Items.OfType<OrientedImageryViewToolbarViewModelBase>())
        {
            toolbarVM.MainViewModel = viewModel;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        foreach (var toolbarVM in e.OldItems?.OfType<OrientedImageryViewToolbarViewModelBase>() ?? [])
        {
            toolbarVM.MainViewModel = null;
        }

        SetToolbarViewModels(ViewModel);
    }

    /// <inheritdoc />
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);

        foreach (var toolbarVM in oldValue?.OfType<OrientedImageryViewToolbarViewModelBase>() ?? [])
        {
            toolbarVM.MainViewModel = null;
        }

        SetToolbarViewModels(ViewModel);
    }
}

#endif