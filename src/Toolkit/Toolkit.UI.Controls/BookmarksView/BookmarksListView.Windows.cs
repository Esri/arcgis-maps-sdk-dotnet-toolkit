#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping;
#if WPF
using System.Windows.Automation;
using System.Windows.Data;
#else
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal sealed partial class BookmarksListView : ListView
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is ListViewItem listViewItem && item is Bookmark bookmark)
            {
                listViewItem.SetBinding(AutomationProperties.NameProperty, new Binding
                {
                    Path = new PropertyPath(nameof(Bookmark.Name)),
                    Mode = BindingMode.OneWay,
                    Source = bookmark,
                });
            }
        }
    }
}
#endif
