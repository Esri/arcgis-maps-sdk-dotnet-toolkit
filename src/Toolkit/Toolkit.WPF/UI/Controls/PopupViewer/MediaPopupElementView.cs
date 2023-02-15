using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public class MediaPopupElementView : Control
    {
        public MediaPopupElementView()
        {
            DefaultStyleKey = typeof(MediaPopupElementView);
        }

        public MediaPopupElement? Element
        {
            get { return GetValue(ElementProperty) as MediaPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(MediaPopupElement), typeof(MediaPopupElementView), new PropertyMetadata(null, (s, e) => ((MediaPopupElementView)s).OnElementPropertyChanged()));

        private void OnElementPropertyChanged()
        {
        }
    }
}