using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public class FieldsPopupElementView : Control
    {
        public FieldsPopupElementView()
        {
            DefaultStyleKey = typeof(FieldsPopupElementView);
        }

        public FieldsPopupElement? Element
        {
            get { return GetValue(ElementProperty) as FieldsPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldsPopupElement), typeof(FieldsPopupElementView), new PropertyMetadata(null, (s, e) => ((FieldsPopupElementView)s).OnGeoElementPropertyChanged()));

        public GeoElement? GeoElement
        {
            get { return GetValue(GeoElementProperty) as GeoElement; }
            set { SetValue(GeoElementProperty, value); }
        }

        public static readonly DependencyProperty GeoElementProperty =
            DependencyProperty.Register(nameof(GeoElement), typeof(GeoElement), typeof(FieldsPopupElementView), new PropertyMetadata(null, (s, e) => ((FieldsPopupElementView)s).OnGeoElementPropertyChanged()));

        private void OnGeoElementPropertyChanged()
        {
        }
    }
}