#if WPF

using System.Collections;
using System.Collections.Specialized;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageDisplayName, Type = typeof(OrientedImageDisplay))]
public partial class OrientedImageryView : ItemsControl
{
    private OrientedImageryViewTemplateSelector? _defaultItemTemplateSelector;

    private void SetToolbarViewModels(OrientedImageryViewModel? viewModel)
    {
        foreach (var toolbarVM in Items.OfType<OrientedImageryToolbarItemBase>())
        {
            toolbarVM.MainViewModel = viewModel;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        foreach (var toolbarVM in e.OldItems?.OfType<OrientedImageryToolbarItemBase>() ?? [])
        {
            toolbarVM.MainViewModel = null;
        }

        SetToolbarViewModels(ViewModel);
    }

    /// <inheritdoc />
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);

        foreach (var toolbarVM in oldValue?.OfType<OrientedImageryToolbarItemBase>() ?? [])
        {
            toolbarVM.MainViewModel = null;
        }

        SetToolbarViewModels(ViewModel);
    }

    /// <inheritdoc />
    protected override void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)
    {
        if (newItemTemplateSelector is not OrientedImageryViewTemplateSelector newSelector)
        {
            return;
        }

        // Save and in the future merge the default selector so the default styles are always available unless overridden.
        if (ReadLocalValue(ItemTemplateSelectorProperty) == DependencyProperty.UnsetValue)
        {
            _defaultItemTemplateSelector = newSelector;
        }
        else if (_defaultItemTemplateSelector is not null)
        {
            newSelector.Merge(_defaultItemTemplateSelector);
        }
    }
}

#endif