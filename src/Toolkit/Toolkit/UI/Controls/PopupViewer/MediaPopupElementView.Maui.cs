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

using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.Maui.PopupViewer"/> control,
    /// used for rendering a <see cref="MediaPopupElement"/>.
    /// </summary>
    public partial class MediaPopupElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static MediaPopupElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            StackLayout root = new StackLayout();
            Label roottitle = new Label();
            roottitle.SetBinding(Label.TextProperty, new Binding("Element.Title", source: RelativeBindingSource.TemplatedParent));
            root.Add(roottitle);
            Label rootcaption = new Label();
            rootcaption.SetBinding(Label.TextProperty, new Binding("Element.Caption", source: RelativeBindingSource.TemplatedParent));
            root.Add(rootcaption);
            CarouselView cv = new CarouselView();
            cv.SetBinding(CarouselView.ItemsSourceProperty, new Binding("Element.Media", source: RelativeBindingSource.TemplatedParent));
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            IndicatorView iv = new IndicatorView();
            cv.IndicatorView = iv;
            root.Add(cv);
            root.Add(iv);
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
            layout.Add(title);
            Label caption = new Label();
            caption.SetBinding(Label.TextProperty, "Caption");
            layout.Add(caption);
            return layout;
        }
    }
}
#endif