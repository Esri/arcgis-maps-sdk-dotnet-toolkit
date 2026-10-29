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
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class NavigationSubView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static NavigationSubView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            var root = new Grid();
            root.SetHighContrastColor(Grid.BackgroundColorProperty, null, HighContrastColorRole.Background);
            root.SetBinding(Grid.BackgroundProperty, static (NavigationSubView view) => view.Background, source: RelativeBindingSource.TemplatedParent);
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            // Use a Grid rather than a HorizontalStackLayout so the header is constrained to the remaining width and can wrap
            Grid topheader = new Grid();
            topheader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            topheader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            topheader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var navigateBack = new Button() { Text =  ToolkitIcons.ChevronLeft, FontFamily = ToolkitIcons.FontFamilyName, BackgroundColor = Colors.Transparent, BorderWidth = 0 };
            navigateBack.SetHighContrastColor(Button.TextColorProperty, Colors.Black, Colors.White);
            var navigateUp = new Button() { Text = ToolkitIcons.ChevronUp, FontFamily = ToolkitIcons.FontFamilyName, BackgroundColor = Colors.Transparent, BorderWidth = 0 };
            navigateUp.SetHighContrastColor(Button.TextColorProperty, Colors.Black, Colors.White);
            topheader.Children.Add(navigateBack);
            Grid.SetColumn(navigateUp, 1);
            topheader.Children.Add(navigateUp);
            ContentControl header = new ContentControl() { VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true) };
            Grid.SetColumn(header, 2);
            header.SetBinding(ContentControl.ContentTemplateProperty, static (NavigationSubView view) => view.HeaderTemplateSelector, source: RelativeBindingSource.TemplatedParent);
            topheader.Children.Add(header);
            root.Children.Add(topheader);
            ScrollView scrollview = new ScrollView();
#if __IOS__
            scrollview.HandlerChanged += static (sender, _) =>
            {
                if (sender is ScrollView { Handler.PlatformView: UIKit.UIScrollView nativeScrollView })
                    nativeScrollView.AllowsKeyboardScrolling = true;
            };
#endif
            scrollview.SetBinding(ScrollView.VerticalScrollBarVisibilityProperty, static (NavigationSubView viewer) => viewer.VerticalScrollBarVisibility, source: RelativeBindingSource.TemplatedParent);
            Grid.SetRow(scrollview, 1);
            ContentControl content = new ContentControl() { VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true) };
            content.SetBinding(ContentControl.ContentTemplateProperty, static (NavigationSubView view) => view.ContentTemplateSelector, source: RelativeBindingSource.TemplatedParent);
            scrollview.Content = content;
            root.Children.Add(scrollview);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("NavigateBack", navigateBack);
            nameScope.RegisterName("NavigateUp", navigateUp);
            nameScope.RegisterName("Header", header);
            nameScope.RegisterName("Content", content);
            nameScope.RegisterName("ScrollViewer", scrollview);
            return root;
        }

        internal Task ScrollToAsync(Element element, ScrollToPosition position, bool animated)
        {
            if (GetTemplateChild("ScrollViewer") is ScrollView scrollView)
            {
                return scrollView.ScrollToAsync(element, position, animated);
            }
            return Task.CompletedTask;
        }
    }
}
#endif