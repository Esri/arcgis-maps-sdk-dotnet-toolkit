#if WPF // Limiting this to WPF for now to keep things simple

using System.Collections;
using System.Collections.Specialized;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageDisplayName, Type = typeof(OrientedImageDisplay))]
public partial class OrientedImageryView : ItemsControl
{
    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        foreach (var toolbarVM in e.OldItems?.OfType<OrientedImageryViewToolbarVM>() ?? [])
        {
            toolbarVM.MainViewModel = null;
        }

        foreach (var toolbarVM in e.NewItems?.OfType<OrientedImageryViewToolbarVM>() ?? [])
        {
            toolbarVM.MainViewModel = this.ViewModel;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);

        try
        {

            foreach (var toolbarVM in oldValue?.OfType<OrientedImageryViewToolbarVM>() ?? [])
            {
                toolbarVM.MainViewModel = null;
            }

            foreach (var toolbarVM in newValue?.OfType<OrientedImageryViewToolbarVM>() ?? [])
            {
                toolbarVM.MainViewModel = this.ViewModel;
            }
        }
        catch (Exception ex) {
            var hi = 1;
        }
    }
}

#endif