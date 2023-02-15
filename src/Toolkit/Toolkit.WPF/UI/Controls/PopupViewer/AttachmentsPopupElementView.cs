using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public class AttachmentsPopupElementView : Control
    {
        public AttachmentsPopupElementView()
        {
            DefaultStyleKey = typeof(AttachmentsPopupElementView);
        }

        public AttachmentsPopupElement? Element
        {
            get { return GetValue(ElementProperty) as AttachmentsPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(AttachmentsPopupElement), typeof(AttachmentsPopupElementView), new PropertyMetadata(null, (s, e) => ((AttachmentsPopupElementView)s).OnElementPropertyChanged()));

        private void OnElementPropertyChanged()
        {
        }
    }
}