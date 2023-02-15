using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public class PopupElementItemsControl : ItemsControl
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if(item is TextPopupElement)
            {
                ((ContentPresenter)element).ContentTemplate = TextPopupElementTemplate;
            }
            else if (item is MediaPopupElement)
            {
                ((ContentPresenter)element).ContentTemplate = MediaPopupElementTemplate;
            }
            else if(item is FieldsPopupElement)
            {
                ((ContentPresenter)element).ContentTemplate = FieldsPopupElementTemplate;
            }
            else if (item is AttachmentsPopupElement)
            {
                ((ContentPresenter)element).ContentTemplate = AttachmentsPopupElementTemplate;
            }
            else if (item is ExpressionPopupElement)
            {
                // TODO
                Debugger.Break();
            }
            base.PrepareContainerForItemOverride(element, item);
        }

        public DataTemplate TextPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(TextPopupElementTemplateProperty); }
            set { SetValue(TextPopupElementTemplateProperty, value); }
        }

        public static readonly DependencyProperty TextPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(TextPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));


        public DataTemplate MediaPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(MediaPopupElementTemplateProperty); }
            set { SetValue(MediaPopupElementTemplateProperty, value); }
        }

        public static readonly DependencyProperty MediaPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(MediaPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));


        public DataTemplate FieldsPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(FieldsPopupElementTemplateProperty); }
            set { SetValue(FieldsPopupElementTemplateProperty, value); }
        }

        public static readonly DependencyProperty FieldsPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(FieldsPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));


        public DataTemplate AttachmentsPopupElementTemplate
        {
            get { return (DataTemplate)GetValue(AttachmentsPopupElementTemplateProperty); }
            set { SetValue(AttachmentsPopupElementTemplateProperty, value); }
        }

        public static readonly DependencyProperty AttachmentsPopupElementTemplateProperty =
            DependencyProperty.Register(nameof(AttachmentsPopupElementTemplate), typeof(DataTemplate), typeof(PopupElementItemsControl), new PropertyMetadata(null));

    }
}
