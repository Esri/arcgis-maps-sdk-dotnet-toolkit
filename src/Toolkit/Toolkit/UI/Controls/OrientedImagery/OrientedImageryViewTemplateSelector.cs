#if WPF || WINDOWS_XAML

using System.Collections.ObjectModel;
#if WPF
using System.Windows.Markup;
#elif WINDOWS_XAML
using Microsoft.UI.Xaml.Markup;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// A <see cref="DataTemplateSelector"/> to define explicit type-template pairs for the toolbar view models used in the <see cref="OrientedImageryView"/>.
/// </summary>
/// <remarks>
/// An <see cref="OrientedImageryView"/> ensures that its <see cref="OrientedImageryViewTemplateSelector"/> always contains the type-template pairs
/// for the default toolbar items unless they have been overridden by the user.
/// </remarks>
#if WPF
[ContentProperty(nameof(TypeTemplatePairs))]
#endif
public partial class OrientedImageryViewTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewTemplateSelector"/> class.
    /// </summary>
    public OrientedImageryViewTemplateSelector()
    {
        TypeTemplatePairs = new ObservableCollection<OrientedImageryViewTemplateSelectorItem>();
        ((ObservableCollection<OrientedImageryViewTemplateSelectorItem>)TypeTemplatePairs).CollectionChanged += (s, e) =>
        {
            foreach (var newItem in e.NewItems?.OfType<OrientedImageryViewTemplateSelectorItem>() ?? [])
            {
                if (TypeTemplatePairs.FirstOrDefault((item) => item.Type == newItem.Type) is OrientedImageryViewTemplateSelectorItem existingItem)
                {
                    existingItem.Template = newItem.Template;
                }
            }
        };
    }

    /// <summary>
    /// Gets the collection of type-template pairs used for selecting the appropriate DataTemplate based on the type of the item.
    /// </summary>
    /// <remarks>
    /// Items with duplicate <see cref="OrientedImageryViewTemplateSelectorItem.Type"/> are not allowed in this collection. If an item with a duplicate type is added it will replace the existing item with the same type.
    /// </remarks>
    public Collection<OrientedImageryViewTemplateSelectorItem> TypeTemplatePairs { get; private set; }

    /// <inheritdoc/>
#if WPF
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
#elif WINDOWS_XAML
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
#endif
    {
        return SelectTemplateInner(item) ??
#if WPF
            base.SelectTemplate(item, container);
#elif WINDOWS_XAML
            base.SelectTemplateCore(item, container);
#endif
    }

#if WINDOWS_XAML
    /// <inheritdoc/>
    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateInner(item) ?? base.SelectTemplateCore(item);
    }
#endif

    private DataTemplate? SelectTemplateInner(object item)
    {
        foreach (var pair in TypeTemplatePairs)
        {
            if (pair.Type.IsInstanceOfType(item))
            {
                return pair.Template;
            }
        }

        return null;
    }

    /// <summary>
    /// Merges the type-template pairs from another <see cref="OrientedImageryViewTemplateSelector"/> into this instance.
    /// </summary>
    /// <remarks>
    /// If a type on <paramref name="other"/> is already registered on this instance, it will be skipped.
    /// </remarks>
    public void Merge(OrientedImageryViewTemplateSelector other)
    {
        foreach (var pair in other.TypeTemplatePairs)
        {
            if (!TypeTemplatePairs.Any(p => p.Type == pair.Type))
            {
                TypeTemplatePairs.Add(pair);
            }
        }
    }
}

/// <summary>
/// Represents a pair of a <see cref="System.Type"/> and its corresponding <see cref="DataTemplate"/> for use in an <see cref="OrientedImageryViewTemplateSelector"/>.
/// </summary>
public class OrientedImageryViewTemplateSelectorItem
{
    /// <summary>
    /// Gets or sets the type associated with this template.
    /// </summary>
    public Type Type { get; set; } = typeof(object);

    /// <summary>
    /// Gets or sets the template associated with this type.
    /// </summary>
    public DataTemplate? Template { get; set; } = null;
}
#endif
