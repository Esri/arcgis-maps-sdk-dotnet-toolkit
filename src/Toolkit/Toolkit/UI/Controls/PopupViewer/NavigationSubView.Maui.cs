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

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class NavigationSubView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private ContentPresenter? _contentView;

        static NavigationSubView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var root = new Grid();
            root.SetBinding(Grid.BackgroundProperty, static (NavigationSubView view) => view.Background, source: RelativeBindingSource.TemplatedParent);
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            HorizontalStackLayout topheader = new HorizontalStackLayout();
            var navigateBack = new Button() { Text = "Back" };
            var navigateUp = new Button() { Text = "Back" };
            topheader.Children.Add(navigateBack);
            topheader.Children.Add(navigateUp);
            ContentControl header = new ContentControl() { VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true) };
            header.SetBinding(ContentControl.ContentTemplateSelectorProperty, static (NavigationSubView view) => view.HeaderTemplateSelector, source: RelativeBindingSource.TemplatedParent);
            topheader.Children.Add(header);
            root.Children.Add(topheader);
            ScrollView scrollview = new ScrollView();
            scrollview.SetBinding(ScrollView.VerticalScrollBarVisibilityProperty, static (NavigationSubView viewer) => viewer.VerticalScrollBarVisibility, source: RelativeBindingSource.TemplatedParent);
            Grid.SetRow(scrollview, 1);
            ContentControl content = new ContentControl() { VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true) };
            content.SetBinding(ContentControl.ContentTemplateSelectorProperty, static (NavigationSubView view) => view.ContentTemplateSelector, source: RelativeBindingSource.TemplatedParent);
            scrollview.Content = content;
            root.Children.Add(scrollview);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("NavigateBack", navigateBack);
            nameScope.RegisterName("NavigateUp", navigateUp);
            nameScope.RegisterName("Header", header);
            nameScope.RegisterName("Content", content);
            return root;
        }
    }
}
#endif