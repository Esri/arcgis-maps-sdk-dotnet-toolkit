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
            roottitle.SetBinding(Label.TextProperty, static (MediaPopupElementView view) => view.Element?.Title, source: RelativeBindingSource.TemplatedParent);
            roottitle.SetBinding(VisualElement.IsVisibleProperty, static (MediaPopupElementView view) => view.Element?.Title, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            roottitle.Style = PopupViewer.GetPopupViewerTitleStyle();
            root.Add(roottitle);
            Label rootcaption = new Label();
            rootcaption.SetBinding(Label.TextProperty, static (MediaPopupElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            rootcaption.SetBinding(VisualElement.IsVisibleProperty, static (MediaPopupElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            rootcaption.Style = PopupViewer.GetPopupViewerCaptionStyle();
#if WINDOWS
            SemanticProperties.SetHeadingLevel(roottitle, SemanticHeadingLevel.Level1);
            SemanticProperties.SetHeadingLevel(rootcaption, SemanticHeadingLevel.Level2);
#endif
            root.Add(rootcaption);
#if WINDOWS
            CarouselView2 cv = new CarouselView2();
            cv.SetBinding(CarouselView2.ItemsSourceProperty, static (MediaPopupElementView view) => view.Element?.Media, source: RelativeBindingSource.TemplatedParent);
#else
            CarouselView cv = new CarouselView();
            cv.SetBinding(CarouselView.ItemsSourceProperty, static (MediaPopupElementView view) => view.Element?.Media, source: RelativeBindingSource.TemplatedParent);
#endif
#if __IOS__ // Workaround for https://github.com/dotnet/maui/issues/12911
            cv.HeightRequest = 300;
#endif
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            IndicatorView iv = new IndicatorView() { HorizontalOptions = LayoutOptions.Center };
#if WINDOWS
            iv.HandlerChanged += (_, _) =>
            {
                if (iv.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement platformView)
                    platformView.IsTabStop = false;
            };
#endif
            cv.IndicatorView = iv;
#if __IOS__
            Grid carouselHost = new Grid();
            carouselHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            carouselHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            carouselHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            Button previous = new Button()
            {
                WidthRequest = 40,
                Padding = 0,
                BorderWidth = 0,
                BackgroundColor = Colors.Transparent,
                Text = ToolkitIcons.ChevronLeft,
                FontFamily = ToolkitIcons.FontFamilyName,
                VerticalOptions = LayoutOptions.Center,
                AutomationId = "PreviousMediaButton",
            };
            SemanticProperties.SetDescription(previous, Properties.Resources.GetString("PopupViewerPreviousMediaButtonText"));
            previous.Clicked += (_, _) =>
            {
                int count = cv.ItemsSource?.Cast<object>().Count() ?? 0;
                if (count > 0)
                    cv.Position = cv.Position > 0 ? cv.Position - 1 : count - 1;
            };
            carouselHost.Add(previous);

            Grid.SetColumn(cv, 1);
            carouselHost.Add(cv);

            Button next = new Button()
            {
                WidthRequest = 40,
                Padding = 0,
                BorderWidth = 0,
                BackgroundColor = Colors.Transparent,
                Text = ToolkitIcons.ChevronRight,
                FontFamily = ToolkitIcons.FontFamilyName,
                VerticalOptions = LayoutOptions.Center,
                AutomationId = "NextMediaButton",
            };
            SemanticProperties.SetDescription(next, Properties.Resources.GetString("PopupViewerNextMediaButtonText"));
            next.Clicked += (_, _) =>
            {
                int count = cv.ItemsSource?.Cast<object>().Count() ?? 0;
                if (count > 0)
                    cv.Position = (cv.Position + 1) % count;
            };
            Grid.SetColumn(next, 2);
            carouselHost.Add(next);

            root.Add(carouselHost);
            root.Add(iv);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(CarouselName, cv);
            nameScope.RegisterName("PreviousButton", previous);
            nameScope.RegisterName("NextButton", next);
            return root;
#else
            root.Add(cv);
            root.Add(iv);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(CarouselName, cv);
            return root;
#endif
        }

        private static object BuildDefaultItemTemplate()
        {
            var pm = new PopupMediaView();
            Grid layout = new Grid();
#if !__IOS__
            layout.HeightRequest = 300;
#endif
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            pm.SetBinding(PopupMediaView.PopupMediaProperty, static (PopupMedia media) => media);
            layout.Add(pm);
            Label title = new Label();
            title.SetBinding(Label.TextProperty, static (PopupMedia media) => media.Title);
            title.SetBinding(VisualElement.IsVisibleProperty, static (PopupMedia media) => media.Title, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            layout.Add(title);
            Grid.SetRow(title, 1);
            Label caption = new Label();
            caption.SetBinding(Label.TextProperty, static (PopupMedia media) => media.Caption);
            caption.SetBinding(VisualElement.IsVisibleProperty, static (PopupMedia media) => media.Caption, converter: Internal.EmptyToFalseConverter.Instance);
            caption.Style = PopupViewer.GetPopupViewerCaptionStyle();
            Grid.SetRow(caption, 2);
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
#if __IOS__
            if (GetTemplateChild("PreviousButton") is Button previous)
                previous.IsVisible = count > 1;
            if (GetTemplateChild("NextButton") is Button next)
                next.IsVisible = count > 1;
#endif
        }

#if __IOS__
        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            OnElementPropertyChanged();
        }
#endif
    }
}
#endif