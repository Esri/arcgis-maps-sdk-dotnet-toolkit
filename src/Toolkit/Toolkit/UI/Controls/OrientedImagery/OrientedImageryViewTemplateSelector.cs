#if WPF

using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

/// <summary>
/// An implementation of <see cref="DataTemplateSelector"/> for selecting templates based on a set of predefined key-template pairs.
/// </summary>
[ContentProperty(nameof(SelectionPairs))]
public class OrientedImageryViewToolbarSelector : DataTemplateSelector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryViewToolbarSelector"/> class.
    /// </summary>
    public OrientedImageryViewToolbarSelector()
    {
        SelectionPairs = new TemplateCollection();
    }

    /// <summary>
    /// Gets the collection of key-template pairs used when selecting an appropriate DataTemplate.
    /// </summary>
    /// <remarks>
    /// Pairs with duplicate <see cref="OrientedImageryToolbarSelectorPair.BaseKey"/>s are not allowed in this collection. If a
    /// pair with a duplicate key is added it will replace the existing pair with the same key.
    /// </remarks>
    public Collection<OrientedImageryToolbarSelectorPair> SelectionPairs { get; private set; }

    /// <inheritdoc/>
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is OrientedImageryToolbarCategory category)
        {
            foreach (var pair in SelectionPairs.Where(p => p.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Category))
            {
                if ((string?)pair.BaseKey != null && (string)pair.BaseKey == category.Name)
                {
                    return pair.Template;
                }
            }
        }
        else if (item is OrientedImageryToolbarItemBase)
        {
            foreach (var pair in SelectionPairs.Where(p => p.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Item))
            {
                if ((Type?)pair.BaseKey != null && ((Type)pair.BaseKey).IsInstanceOfType(item))
                {
                    return pair.Template;
                }
            }
        }

        return base.SelectTemplate(item, container);
    }

    /// <summary>
    /// Merges the key-template pairs from another <see cref="OrientedImageryViewToolbarSelector"/> into this instance.
    /// </summary>
    /// <remarks>
    /// If a key on <paramref name="other"/> is already registered on this instance, it will be skipped.
    /// Entries are copied so changing a merged entry does not modify the source selector. The templates themselves are shared.
    /// </remarks>
    public void Merge(OrientedImageryViewToolbarSelector other)
    {
        foreach (var pair in other.SelectionPairs)
        {
            if (SelectionPairs.Any(p => SelectorKeysAreEqual(p, pair)))
                continue;

            if (pair.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Category)
                SelectionPairs.Add(new OrientedImageryToolbarCategorySelectorPair { CategoryName = (string?)pair.BaseKey, Template = pair.Template });
            else if (pair.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Item)
                SelectionPairs.Add(new OrientedImageryToolbarItemSelectorPair { ItemType = (Type?)pair.BaseKey, Template = pair.Template });
        }
    }

    internal static bool SelectorKeysAreEqual(OrientedImageryToolbarSelectorPair itemA, OrientedImageryToolbarSelectorPair itemB)
    {
        if (itemA.TargetType != itemB.TargetType)
            return false;

        if (itemA.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Category)
            return (string?)itemA.BaseKey == (string?)itemB.BaseKey;

        if (itemA.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Item)
            return (Type?)itemA.BaseKey == (Type?)itemB.BaseKey;

        return false;
    }

    private sealed class TemplateCollection : ObservableCollection<OrientedImageryToolbarSelectorPair>
    {
        protected override void InsertItem(int index, OrientedImageryToolbarSelectorPair item)
        {
            var existing = GetExistingEntry(item);
            if (existing != null)
                base.SetItem(IndexOf(existing), item);
            else
                base.InsertItem(index, item);
        }

        protected override void SetItem(int index, OrientedImageryToolbarSelectorPair item)
        {
            var existing = GetExistingEntry(item);
            int duplicateIndex = existing == null ? -1 : IndexOf(existing);
            if (duplicateIndex >= 0 && duplicateIndex != index)
            {
                RemoveItem(duplicateIndex);
                if (duplicateIndex < index)
                    index--;
            }
            base.SetItem(index, item);
        }

        private OrientedImageryToolbarSelectorPair? GetExistingEntry(OrientedImageryToolbarSelectorPair newItem)
        {
            OrientedImageryToolbarSelectorPair? existing = null;
            if (newItem.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Category)
                existing = this.FirstOrDefault(pair => SelectorKeysAreEqual(pair, newItem));
            else if (newItem.TargetType == OrientedImageryToolbarSelectorPair.ToolbarType.Item)
                existing = this.FirstOrDefault(pair => SelectorKeysAreEqual(pair, newItem));
            return existing;
        }
    }
}

/// <summary>
/// An <see cref="OrientedImageryToolbarSelectorPair"/> for <see cref="OrientedImageryToolbarCategory"/> objects.
/// </summary>
public sealed class OrientedImageryToolbarCategorySelectorPair : OrientedImageryToolbarSelectorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryToolbarCategorySelectorPair"/> class.
    /// </summary>
    public OrientedImageryToolbarCategorySelectorPair() : base(ToolbarType.Category) { }

    /// <summary>
    /// The <see cref="OrientedImageryToolbarCategory.Name"/> of the toolbar category this selector item applies to.
    /// </summary>
    public string? CategoryName
    {
        get => (string?)BaseKey;
        set => BaseKey = (string?)value;
    }
}

/// <summary>
/// An <see cref="OrientedImageryToolbarSelectorPair"/> for <see cref="OrientedImageryToolbarItemBase"/> objects.
/// </summary>
public sealed class OrientedImageryToolbarItemSelectorPair : OrientedImageryToolbarSelectorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryToolbarCategorySelectorPair"/> class.
    /// </summary>
    public OrientedImageryToolbarItemSelectorPair() : base(OrientedImageryToolbarSelectorPair.ToolbarType.Item) { }

    /// <summary>
    /// The <see cref="Type"/> of the toolbar item this pair applies to.
    /// </summary>
    public Type? ItemType
    {
        get => (Type?)BaseKey;
        set => BaseKey = (Type?)value;
    }
}

/// <summary>
/// Represents a <see cref="DataTemplate"/> object paired to some key value of type <see paramref="T"/> for use in an <see cref="OrientedImageryViewToolbarSelector"/>.
/// </summary>
public class OrientedImageryToolbarSelectorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrientedImageryToolbarSelectorPair"/> class.
    /// </summary>
    protected OrientedImageryToolbarSelectorPair(ToolbarType targetType)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// Gets or sets the key object associated with this template.
    /// </summary>
    /// <remarks>
    /// This property is not meant to be set publically, and should instead be set using wrapper properties
    /// in an inherited class. See <see cref="OrientedImageryToolbarItemSelectorPair.ItemType"/> and
    /// <see cref="OrientedImageryToolbarCategorySelectorPair.CategoryName"/>.
    /// </remarks>
    internal protected object? BaseKey { get; protected set; }

    /// <summary>
    /// Gets or sets the template associated with this type.
    /// </summary>
    public DataTemplate Template { get; set; } = new DataTemplate();

    /// <summary>
    /// The object type the selector item applies to.
    /// </summary>
    public ToolbarType TargetType { get; protected set; }

    /// <summary>
    /// An enum representing either a <see cref="OrientedImageryToolbarCategory"/> or a <see cref="OrientedImageryToolbarItemBase"/>.
    /// </summary>
    public enum ToolbarType
    {
        /// <summary>An <see cref="OrientedImageryToolbarCategory"/></summary>
        Category,

        /// <summary>An <see cref="OrientedImageryToolbarItemBase"/></summary>
        Item
    }
}
#endif
