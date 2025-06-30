#if MAUI
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    internal class ContentControl : ContentPresenter
    {
        private DataTemplate? currentTemplate;
        
        public ContentControl()
        {
        }

        private void UpdateData()
        {
            if (ContentData is null)
                Content = null;
            else
            {
                var template = ContentTemplate.SelectDataTemplate(ContentData, this);
                if (template is null)
                {
                    Content = null;
                    return;
                }

                if (currentTemplate != template) //Don't create a new view if the template isn't changing.
                {
                    currentTemplate = template;
                    var view = template.CreateContent(ContentData, this) as View;
                    if (view is null)
                    {
                        Content = null;
                        return;
                    }
                    view.BindingContext = ContentData;
                    Content = view;
                }
                else  if (Content is not null && Content.BindingContext != ContentData)
                    Content.BindingContext = ContentData;
            }
        }

        /// <summary>
        /// Gets or sets the template for the header.
        /// </summary>
        public DataTemplate? ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty) as DataTemplateSelector; }
            set { SetValue(ContentTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentTemplateProperty = Toolkit.Internal.PropertyHelper.CreateProperty<DataTemplate, ContentControl>(
            nameof(ContentTemplate), propertyChanged: (cc, oldvalue, newvalue) => cc.UpdateData());

        private object? _content;

        public object? ContentData
        {
            get => GetValue(ContentDataProperty);
            set => SetValue(ContentDataProperty, value);
        }

        public static readonly DependencyProperty ContentDataProperty = Toolkit.Internal.PropertyHelper.CreateProperty<object, ContentControl>(
           nameof(ContentData), propertyChanged: (cc, oldvalue, newvalue) => cc.UpdateData());
    }
}
#endif
