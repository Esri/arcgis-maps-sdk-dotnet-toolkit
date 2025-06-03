using System;
using System.Collections.Generic;
using System.Text;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Template selector for selecting the correct datatemplate for each datatype displayed on each page.
    /// </summary>
    public partial class FeatureFormContentTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Initializes an instance of the <see cref="FeatureFormContentTemplateSelector"/> class.
        /// </summary>
        public FeatureFormContentTemplateSelector()
        {
        }

        /// <inheritdoc />
#if WINDOWS_XAML
        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
#elif MAUI
        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
#else
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
#endif
        {
            if (item is Esri.ArcGISRuntime.Mapping.FeatureForms.FeatureForm)
                return FeatureFormTemplate;
            if(item is UtilityNetworks.UtilityAssociationsFilterResult)
                return UtilityAssociationsFilterResultTemplate;
#if WINDOWS_XAML
            return base.SelectTemplateCore(item, container);
#elif MAUI
            return null;
#else
            return base.SelectTemplate(item, container);
#endif
        }

        /// <summary>
        /// Gets or sets the template used for rendering a <see cref="Mapping.FeatureForms.FeatureForm"/>
        /// </summary>
        public DataTemplate? FeatureFormTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used for rendering a <see cref="UtilityNetworks.UtilityAssociationsFilterResult"/>
        /// </summary>
        public DataTemplate? UtilityAssociationsFilterResultTemplate { get; set; }
    }
}