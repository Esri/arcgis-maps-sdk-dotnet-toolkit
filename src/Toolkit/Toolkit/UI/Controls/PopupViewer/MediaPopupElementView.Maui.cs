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
#if WINDOWS || __IOS__ || __ANDROID__
            SemanticProperties.SetHeadingLevel(roottitle, SemanticHeadingLevel.Level1);
#endif
#if WINDOWS
            SemanticProperties.SetHeadingLevel(rootcaption, SemanticHeadingLevel.Level2);
#endif
            root.Add(rootcaption);
#if WINDOWS
            CarouselView2 cv = new CarouselView2();
            cv.SetBinding(CarouselView2.ItemsSourceProperty, static (MediaPopupElementView view) => view.Element?.Media, source: RelativeBindingSource.TemplatedParent);
#else
            CarouselView cv = new CarouselView();
#if __IOS__
            cv.Loop = false;
#endif
            cv.SetBinding(CarouselView.ItemsSourceProperty, static (MediaPopupElementView view) => view.Element?.Media, source: RelativeBindingSource.TemplatedParent);
#endif
#if __IOS__ // Workaround for https://github.com/dotnet/maui/issues/12911
            cv.HeightRequest = 300;
#endif
#if __ANDROID__
            Internal.PopupKeyboardNavigation.Attach(cv);
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

            Button previous = CreateMediaNavigationButton(ToolkitIcons.ChevronLeft, "PopupViewerPreviousMediaButtonText", "PreviousMediaButton");
            previous.Clicked += (_, _) =>
            {
                if (cv.ItemsSource is IList<PopupMedia> media && cv.Position > 0 && cv.Position < media.Count)
                    cv.Position--;
            };
            carouselHost.Add(previous);

            Grid.SetColumn(cv, 1);
            carouselHost.Add(cv);

            Button next = CreateMediaNavigationButton(ToolkitIcons.ChevronRight, "PopupViewerNextMediaButtonText", "NextMediaButton");
            next.Clicked += (_, _) =>
            {
                if (cv.ItemsSource is IList<PopupMedia> media && cv.Position >= 0 && cv.Position < media.Count - 1)
                    cv.Position++;
            };
            Grid.SetColumn(next, 2);
            carouselHost.Add(next);

            void UpdateNavigationButtons()
            {
                int count = (cv.ItemsSource as IList<PopupMedia>)?.Count ?? 0;
                previous.IsVisible = next.IsVisible = count > 1;
                previous.IsEnabled = count > 1 && cv.Position > 0 && cv.Position < count;
                next.IsEnabled = count > 1 && cv.Position >= 0 && cv.Position < count - 1;
            }
            cv.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(CarouselView.Position) or nameof(CarouselView.ItemsSource))
                    UpdateNavigationButtons();
            };
            UpdateNavigationButtons();

            root.Add(carouselHost);
#elif __ANDROID__
            root.Add(new Internal.PopupMediaPager(cv, iv) { Spacing = root.Spacing });
#else
            root.Add(cv);
#endif
#if !__ANDROID__
            root.Add(iv);
#endif
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(CarouselName, cv);
#if __IOS__
            nameScope.RegisterName("PreviousButton", previous);
            nameScope.RegisterName("NextButton", next);
#endif
            return root;
        }

#if __IOS__
        private static Button CreateMediaNavigationButton(string glyph, string descriptionKey, string automationId)
        {
            var button = new Button()
            {
                WidthRequest = 40,
                Padding = 0,
                BorderWidth = 0,
                BackgroundColor = Colors.Transparent,
                Text = glyph,
                FontFamily = ToolkitIcons.FontFamilyName,
                VerticalOptions = LayoutOptions.Center,
                AutomationId = automationId,
            };
            SemanticProperties.SetDescription(button, Properties.Resources.GetString(descriptionKey));
            return button;
        }
#endif

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
#if __IOS__
            SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level2);
#endif
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