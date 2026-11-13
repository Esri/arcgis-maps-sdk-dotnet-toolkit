#if WPF || MAUI

using System.Collections.ObjectModel;
#if WPF
using System.Windows.Markup;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.OrientedImagery;
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
#endif

/// <summary>
/// An implementation of <see cref="DataTemplateSelector"/> for selecting templates based on a set of predefined key-template pairs.
/// </summary>
[ContentProperty(nameof(SelectionPairs))]
public class ToolbarSelector : DataTemplateSelector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolbarSelector"/> class.
    /// </summary>
    public ToolbarSelector()
    {
        SelectionPairs = new TemplateCollection();
    }

    /// <summary>
    /// Gets the collection of key-template pairs used when selecting an appropriate DataTemplate.
    /// </summary>
    /// <remarks>
    /// Pairs with duplicate <see cref="ToolbarSelectorPair.BaseKey"/>s are not allowed in this collection. If a
    /// pair with a duplicate key is added it will replace the existing pair with the same key.
    /// </remarks>
    public Collection<ToolbarSelectorPair> SelectionPairs { get; private set; }

#if MAUI
    // The owning control supplies its platform presentation when no override matches.
    internal static DataTemplate DefaultTemplate { get; } = new DataTemplate();

    /// <inheritdoc/>
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
#else
    /// <inheritdoc/>
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
#endif
    {
        if (item is ToolbarCategory category)
        {
            foreach (var pair in SelectionPairs.Where(p => p.TargetType == ToolbarSelectorPair.ToolbarType.Category))
            {
                if ((string?)pair.BaseKey != null && (string)pair.BaseKey == category.Name)
                {
                    return pair.Template;
                }
            }
        }
        else if (item is ToolbarItemBase)
        {
            foreach (var pair in SelectionPairs.Where(p => p.TargetType == ToolbarSelectorPair.ToolbarType.Item))
            {
                if ((Type?)pair.BaseKey != null && ((Type)pair.BaseKey).IsInstanceOfType(item))
                {
                    return pair.Template;
                }
            }
        }

#if MAUI
        return container is OrientedImageryViewer ? DefaultTemplate :
            throw new InvalidOperationException($"No oriented imagery toolbar template is registered for '{item.GetType().Name}'.");
#else
        return base.SelectTemplate(item, container);
#endif
    }

    /// <summary>
    /// Merges the key-template pairs from another <see cref="ToolbarSelector"/> into this instance.
    /// </summary>
    /// <remarks>
    /// If a key on <paramref name="other"/> is already registered on this instance, it will be skipped.
    /// Entries are copied so changing a merged entry does not modify the source selector. The templates themselves are shared.
    /// </remarks>
    public void Merge(ToolbarSelector other)
    {
        foreach (var pair in other.SelectionPairs)
        {
            if (SelectionPairs.Any(p => SelectorKeysAreEqual(p, pair)))
                continue;

            if (pair.TargetType == ToolbarSelectorPair.ToolbarType.Category)
                SelectionPairs.Add(new ToolbarCategorySelectorPair { CategoryName = (string?)pair.BaseKey, Template = pair.Template });
            else if (pair.TargetType == ToolbarSelectorPair.ToolbarType.Item)
                SelectionPairs.Add(new ToolbarItemSelectorPair { ItemType = (Type?)pair.BaseKey, Template = pair.Template });
        }
    }

    internal static bool SelectorKeysAreEqual(ToolbarSelectorPair itemA, ToolbarSelectorPair itemB)
    {
        if (itemA.TargetType != itemB.TargetType)
            return false;

        if (itemA.TargetType == ToolbarSelectorPair.ToolbarType.Category)
            return (string?)itemA.BaseKey == (string?)itemB.BaseKey;

        if (itemA.TargetType == ToolbarSelectorPair.ToolbarType.Item)
            return (Type?)itemA.BaseKey == (Type?)itemB.BaseKey;

        return false;
    }

    private sealed class TemplateCollection : ObservableCollection<ToolbarSelectorPair>
    {
        protected override void InsertItem(int index, ToolbarSelectorPair item)
        {
            var existing = GetExistingEntry(item);
            if (existing != null)
                base.SetItem(IndexOf(existing), item);
            else
                base.InsertItem(index, item);
        }

        protected override void SetItem(int index, ToolbarSelectorPair item)
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

        private ToolbarSelectorPair? GetExistingEntry(ToolbarSelectorPair newItem)
        {
            ToolbarSelectorPair? existing = null;
            if (newItem.TargetType == ToolbarSelectorPair.ToolbarType.Category)
                existing = this.FirstOrDefault(pair => SelectorKeysAreEqual(pair, newItem));
            else if (newItem.TargetType == ToolbarSelectorPair.ToolbarType.Item)
                existing = this.FirstOrDefault(pair => SelectorKeysAreEqual(pair, newItem));
            return existing;
        }
    }
}

/// <summary>
/// An <see cref="ToolbarSelectorPair"/> for <see cref="ToolbarCategory"/> objects.
/// </summary>
public sealed class ToolbarCategorySelectorPair : ToolbarSelectorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolbarCategorySelectorPair"/> class.
    /// </summary>
    public ToolbarCategorySelectorPair() : base(ToolbarType.Category) { }

    /// <summary>
    /// The <see cref="ToolbarCategory.Name"/> of the toolbar category this selector item applies to.
    /// </summary>
    public string? CategoryName
    {
        get => (string?)BaseKey;
        set => BaseKey = (string?)value;
    }
}

/// <summary>
/// An <see cref="ToolbarSelectorPair"/> for <see cref="ToolbarItemBase"/> objects.
/// </summary>
public sealed class ToolbarItemSelectorPair : ToolbarSelectorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolbarCategorySelectorPair"/> class.
    /// </summary>
    public ToolbarItemSelectorPair() : base(ToolbarSelectorPair.ToolbarType.Item) { }

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
/// Represents a <see cref="DataTemplate"/> object paired to some key value of type <see paramref="T"/> for use in an <see cref="ToolbarSelector"/>.
/// </summary>
public class ToolbarSelectorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolbarSelectorPair"/> class.
    /// </summary>
    protected ToolbarSelectorPair(ToolbarType targetType)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// Gets or sets the key object associated with this template.
    /// </summary>
    /// <remarks>
    /// This property is not meant to be set publically, and should instead be set using wrapper properties
    /// in an inherited class. See <see cref="ToolbarItemSelectorPair.ItemType"/> and
    /// <see cref="ToolbarCategorySelectorPair.CategoryName"/>.
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
    /// An enum representing either a <see cref="ToolbarCategory"/> or a <see cref="ToolbarItemBase"/>.
    /// </summary>
    public enum ToolbarType
    {
        /// <summary>An <see cref="ToolbarCategory"/></summary>
        Category,

        /// <summary>An <see cref="ToolbarItemBase"/></summary>
        Item
    }
}
#endif
