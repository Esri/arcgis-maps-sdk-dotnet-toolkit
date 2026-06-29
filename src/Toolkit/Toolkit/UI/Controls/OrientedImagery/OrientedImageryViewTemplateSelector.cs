#if WPF

using System.Windows.Markup;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[ContentProperty(nameof(TypeTemplatePairs))]
public class OrientedImageryViewTemplateSelector : DataTemplateSelector
{
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

    public Collection<OrientedImageryViewTemplateSelectorItem> TypeTemplatePairs { get; set; } = new Collection<OrientedImageryViewTemplateSelectorItem>();
}
public class OrientedImageryViewTemplateSelectorItem
{
    public Type Type { get; set; } = typeof(object);
    public DataTemplate Template { get; set; } = new DataTemplate();
}

#endif