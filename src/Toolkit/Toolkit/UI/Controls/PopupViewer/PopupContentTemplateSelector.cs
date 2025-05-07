#if !MAUI
using System;
using System.Collections.Generic;
using System.Text;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    public partial class PopupContentTemplateSelector : DataTemplateSelector
    {
        public PopupContentTemplateSelector()
        {
        }

#if WINDOWS_XAML
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
#else
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
#endif
        {
            if (item is Esri.ArcGISRuntime.Mapping.Popups.Popup)
                return PopupTemplate;
            if(item is UtilityNetworks.UtilityAssociationsFilterResult)
                return UtilityAssociationsFilterResultTemplate;
#if WINDOWS_XAML
            return base.SelectTemplateCore(item, container);
#else
            return base.SelectTemplate(item, container);
#endif
        }

        public DataTemplate PopupTemplate { get; set; }

        public DataTemplate UtilityAssociationsFilterResultTemplate { get; set; }
    }
}
#endif