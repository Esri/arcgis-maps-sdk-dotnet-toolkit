// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/
#if MAUI

using Microsoft.Maui.Controls.Internals;
using Esri.ArcGISRuntime.Mapping.Popups;
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="MediaPopupElement"/>.
    /// </summary>
    public partial class MediaPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        
        /// <summary>
        /// Name of the carousel control in the template.
        /// </summary>
        public const string CarouselName = "Carousel";

        static MediaPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            StackLayout root = new StackLayout();
            Label roottitle = new Label();
            roottitle.SetBinding(Label.TextProperty, new Binding("Element.Title", source: RelativeBindingSource.TemplatedParent));
            roottitle.SetBinding(VisualElement.IsVisibleProperty, new Binding("Element.Title", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            root.Add(roottitle);
            Label rootcaption = new Label();
            rootcaption.SetBinding(Label.TextProperty, new Binding("Element.Caption", source: RelativeBindingSource.TemplatedParent));
            rootcaption.SetBinding(VisualElement.IsVisibleProperty, new Binding("Element.Caption", source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance));
            root.Add(rootcaption);
            CarouselView cv = new CarouselView();
            cv.SetBinding(CarouselView.ItemsSourceProperty, new Binding("Element.Media", source: RelativeBindingSource.TemplatedParent));
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            IndicatorView iv = new IndicatorView() { HorizontalOptions = LayoutOptions.Center };
            cv.IndicatorView = iv;
            root.Add(cv);
            root.Add(iv);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(CarouselName, cv);
            return root;
        }

        private static object BuildDefaultItemTemplate()
        {
            StackLayout layout = new StackLayout();
            var pm = new PopupMediaView() { HeightRequest = 200 };
            pm.SetBinding(PopupMediaView.PopupMediaProperty, ".");
            layout.Add(pm);
            Label title = new Label();
            title.SetBinding(Label.TextProperty, "Title");
            title.SetBinding(VisualElement.IsVisibleProperty, new Binding("Title", converter: Internal.EmptyToFalseConverter.Instance));
            layout.Add(title);
            Label caption = new Label();
            caption.SetBinding(Label.TextProperty, "Caption");
            caption.SetBinding(VisualElement.IsVisibleProperty, new Binding("Caption", converter: Internal.EmptyToFalseConverter.Instance));
            layout.Add(caption);
            return layout;
        }

        private void OnElementPropertyChanged()
        {
            int count = Element?.Media is null ? 0 : Element.Media.Count;
            var carousel = GetTemplateChild(CarouselName) as CarouselView;
            if(carousel is not null)
            {
                carousel.IsSwipeEnabled = count > 1;
            }
        }
    }
}
#endif