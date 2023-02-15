using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public class TextPopupElementView : Control
    {
        public TextPopupElementView()
        {
            DefaultStyleKey = typeof(TextPopupElementView);
        }

        public TextPopupElement? Element
        {
            get { return GetValue(ElementProperty) as TextPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(TextPopupElement), typeof(TextPopupElementView), new PropertyMetadata(null, (s, e) => ((TextPopupElementView)s).OnElementPropertyChanged()));

        private void OnElementPropertyChanged()
        {
            var rt = GetTemplateChild("TextViewArea") as RichTextBox;
            var html = Element?.Text;
            
        }
    }
}