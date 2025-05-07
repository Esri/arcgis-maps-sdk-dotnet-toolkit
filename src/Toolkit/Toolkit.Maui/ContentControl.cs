#if MAUI
using System;
using System.ComponentModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    internal class ContentControl : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static ContentControl()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var presenter = new ContentPresenter();
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(presenter, nameScope);
            nameScope.RegisterName("ContentPresenter", presenter);
            return presenter;
        }

        public ContentControl()
        {
            ControlTemplate = DefaultControlTemplate;
        }

        protected override void OnApplyTemplate()
        {
            UpdateData();
            base.OnApplyTemplate();
        }

        private void UpdateData()
        {
            if (GetTemplateChild("ContentPresenter") is ContentPresenter presenter)
            {
                var template = ContentTemplateSelector?.SelectTemplate(Content, presenter);
                if (template is not null)
                {
                    var contentView = template.CreateContent() as View;
                    if (contentView is not null)
                        contentView.BindingContext = Content;
                    presenter.Content = contentView;
                }
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

        public object? Content
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