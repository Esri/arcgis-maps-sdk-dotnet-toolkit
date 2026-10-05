#if WPF

using System.Windows.Markup;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// A <see cref="DataTemplateSelector"/> to define explicit type-template pairs for the toolbar view models used in the <see cref="OrientedImageryView"/>.
/// </summary>
/// <remarks>
/// An <see cref="OrientedImageryView"/> ensures that its <see cref="OrientedImageryViewTemplateSelector"/> always contains the type-template pairs
/// for the default toolbar items unless they have been overridden by the user.
/// </remarks>
[ContentProperty(nameof(TypeTemplatePairs))]
public class OrientedImageryViewTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewTemplateSelector"/> class.
    /// </summary>
    public OrientedImageryViewTemplateSelector()
    {
        TypeTemplatePairs = new TemplateCollection();
    }

    /// <summary>
    /// Gets the collection of type-template pairs used for selecting the appropriate DataTemplate based on the type of the item.
    /// </summary>
    /// <remarks>
    /// Items with duplicate <see cref="OrientedImageryViewTemplateSelectorItem.Type"/> are not allowed in this collection. If an item with a duplicate type is added it will replace the existing item with the same type.
    /// Replacing an entry also removes any other entry with the replacement's type.
    /// </remarks>
    public Collection<OrientedImageryViewTemplateSelectorItem> TypeTemplatePairs { get; private set; }

    /// <inheritdoc/>
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        foreach (var pair in TypeTemplatePairs)
        {
            if (pair.Type.IsInstanceOfType(item))
            {
                return pair.Template;
            }
        }

        return base.SelectTemplate(item, container);
    }

    /// <summary>
    /// Merges the type-template pairs from another <see cref="OrientedImageryViewTemplateSelector"/> into this instance.
    /// </summary>
    /// <remarks>
    /// If a type on <paramref name="other"/> is already registered on this instance, it will be skipped.
    /// Entries are copied so changing a merged entry does not modify the source selector. The templates themselves are shared.
    /// </remarks>
    public void Merge(OrientedImageryViewTemplateSelector other)
    {
        foreach (var pair in other.TypeTemplatePairs)
        {
            if (!TypeTemplatePairs.Any(p => p.Type == pair.Type))
            {
                TypeTemplatePairs.Add(new OrientedImageryViewTemplateSelectorItem { Type = pair.Type, Template = pair.Template });
            }
        }
    }

    private sealed class TemplateCollection : ObservableCollection<OrientedImageryViewTemplateSelectorItem>
    {
        protected override void InsertItem(int index, OrientedImageryViewTemplateSelectorItem item)
        {
            var existing = this.FirstOrDefault(pair => pair.Type == item.Type);
            if (existing != null)
                base.SetItem(IndexOf(existing), item);
            else
                base.InsertItem(index, item);
        }

        protected override void SetItem(int index, OrientedImageryViewTemplateSelectorItem item)
        {
            var existing = this.FirstOrDefault(pair => pair.Type == item.Type);
            int duplicateIndex = existing == null ? -1 : IndexOf(existing);
            if (duplicateIndex >= 0 && duplicateIndex != index)
            {
                RemoveItem(duplicateIndex);
                if (duplicateIndex < index)
                    index--;
            }
            base.SetItem(index, item);
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
    public DataTemplate Template { get; set; } = new DataTemplate();
}
#endif
