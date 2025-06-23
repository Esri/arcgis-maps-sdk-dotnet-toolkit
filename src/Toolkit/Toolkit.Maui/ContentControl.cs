#if MAUI
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    internal class ContentControl : ContentPresenter
    {
        public ContentControl()
        {
        }

        private void UpdateData()
        {
            if (ContentData is null)
                Content = null;
            else
            {
                var view = ContentTemplateSelector?.CreateContent(ContentData, this) as View;
                if (view is not null)
                    view.BindingContext = ContentData;

                Content = view;
            }
        }

        /// <summary>
        /// Gets or sets the template selector for the header.
        /// </summary>
        public DataTemplateSelector? ContentTemplateSelector
        {
            get { return GetValue(ContentTemplateSelectorProperty) as DataTemplateSelector; }
            set { SetValue(ContentTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ContentTemplateSelector"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentTemplateSelectorProperty = Toolkit.Internal.PropertyHelper.CreateProperty<DataTemplateSelector, ContentControl>(
            nameof(ContentTemplateSelector), propertyChanged: (cc, oldvalue, newvalue) => cc.UpdateData());

        private object? _content;

        public object? ContentData
        {
            get  => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    UpdateData();
                }
            }
        }
    }
}
#endif
