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

using Esri.ArcGISRuntime.Toolkit.Internal;
#if WPF
#elif WINUI
using Microsoft.UI.Xaml.Media.Animation;
using Key = Windows.System.VirtualKey;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Media.Animation;
using Key = Windows.System.VirtualKey;
#elif MAUI
using ScrollViewer = Microsoft.Maui.Controls.ScrollView;
using ContentControl = Microsoft.Maui.Controls.ContentView;
using FrameworkElement = Microsoft.Maui.Controls.View;
#endif


#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Sub view for the PopupViewer control.
    /// </summary>
    public partial class NavigationSubView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationSubView"/> class.
        /// </summary>
        public NavigationSubView()
        {
#if MAUI
            ControlTemplate = DefaultControlTemplate;
#else
            DefaultStyleKey = typeof(NavigationSubView);
#endif
        }

        private void UpdateView()
        {
            if (GetTemplateChild("NavigateBack") is FrameworkElement back)
            {
#if MAUI
                back.IsVisible = NavigationStack.Count > 0;
#else
                back.Visibility = NavigationStack.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
#endif
#if WPF
                back.Margin = NavigationStack.Count > 1 ? new Thickness() : new Thickness(0, 0, 10, 0);
#endif
            }
            if (GetTemplateChild("NavigateUp") is FrameworkElement up)
            {
#if MAUI
                up.IsVisible = NavigationStack.Count > 1;
#else
                up.Visibility = NavigationStack.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
#endif
            }
        }

        private Stack<Tuple<object,double>> NavigationStack = new Stack<Tuple<object, double>>();
        
        internal void Navigate(object? content, bool clearNavigationStack = false)
        {
            if (content is null && !clearNavigationStack)
                throw new ArgumentNullException(nameof(content));

            double offset = 0;
            if (GetTemplateChild("ScrollViewer") is ScrollViewer sv)
            {
#if MAUI
                offset = sv.ScrollY;
#else
                offset = sv.VerticalOffset;
#endif
            }


            if (clearNavigationStack)
                NavigationStack.Clear();
            else if (Content is not null) // Move current content into the stack
                NavigationStack.Push(new Tuple<object, double>(Content, offset));
#if WINDOWS_XAML
            ContentTransitions = new TransitionCollection();
            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("NavigationSubViewForwardAnimation");
            bool success = false;
            if (animation != null && GetTemplateChild("Header") is UIElement header)
            {
                success = animation.TryStart(header);
            }
            if(!success)
            {
            if (NavigationStack.Count > 0)
                ContentTransitions.Add(new EntranceThemeTransition() { FromHorizontalOffset = 200, FromVerticalOffset = 0 });
            }
#endif
            SetContent(content);

#if MAUI
            (GetTemplateChild("ScrollViewer") as ScrollViewer)?.ScrollToAsync(0,0,false);
#elif WPF
            (GetTemplateChild("ScrollViewer") as ScrollViewer)?.ScrollToHome();
#elif WINDOWS_XAML
            (GetTemplateChild("ScrollViewer") as ScrollViewer)?.ChangeView(null, 0, null, disableAnimation: true);
#endif
        }

        private void SetContent(object? content)
        {
            Content = content;
            if (GetTemplateChild("Header") is ContentControl cc1)
                cc1.Content = content;
            if (GetTemplateChild("Content") is ContentControl cc2)
                cc2.Content = content;
            UpdateView();
        }

        private void GoBack()
        {
            if (NavigationStack.Count == 0)
                return;
            var previousPage = NavigationStack.Pop();
            var content = previousPage.Item1;
            var lastOffset = previousPage.Item2;

#if WINDOWS_XAML
            ContentTransitions = new TransitionCollection
            {
                new EntranceThemeTransition() { FromHorizontalOffset = -200, FromVerticalOffset = 0 }
            };
#endif
            if (GetTemplateChild("ScrollViewer") is ScrollViewer sv)
            {
#if WINUI
                // Restore scroll offset on back navigation
                EventHandler<object>? handler = null;
                handler = (s, e) =>
                {
                    sv.LayoutUpdated -= handler;
                    sv.ChangeView(null, lastOffset, null, true);
                };
                sv.LayoutUpdated += handler;
#elif WPF
                ScrollChangedEventHandler? handler = null;
                handler = (s, e) =>
                {
                    if (e.ExtentHeight >= lastOffset)
                    {
                        sv.ScrollChanged -= handler;
                        sv.ScrollToVerticalOffset(lastOffset);
                    }
                };
                sv.ScrollChanged += handler; 
#elif MAUI
                EventHandler? handler = null;
                handler = (s, e) =>
                {
                    sv.Content.SizeChanged -= handler;
                    _ = sv.ScrollToAsync(0, lastOffset, false);
                };
                sv.Content.SizeChanged += handler;
#endif
            }
            SetContent(content);
        }

        private void GoUp()
        {
            if (NavigationStack.Count == 0)
                return;
            var content = NavigationStack.Last();
            NavigationStack.Clear();
#if WINDOWS_XAML
            ContentTransitions = new TransitionCollection
            {
                new EntranceThemeTransition() { FromHorizontalOffset = 0, FromVerticalOffset = -200 }
            };
#endif
            SetContent(content.Item1);
        }

        /// <inheritdoc />
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#elif WPF
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("NavigateBack") is Button backButton)
            {
#if MAUI
                backButton.Clicked += (s, e) => GoBack();
#else
                backButton.Click += (s, e) => GoBack();
#endif
            }
            if (GetTemplateChild("NavigateUp") is Button upButton)
            {
#if MAUI
                upButton.Clicked += (s, e) => GoUp();
#else
                upButton.Click += (s, e) => GoUp();
#endif
            }
            if (GetTemplateChild("Header") is ContentControl cc1)
                cc1.Content = Content;
            if (GetTemplateChild("Content") is ContentControl cc2)
                cc2.Content = Content;
            UpdateView();
        }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public object? Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = PropertyHelper.CreateProperty<object, NavigationSubView>(nameof(Content), null);

        /// <summary>
        /// Gets or sets the template selector for the content.
        /// </summary>
        public DataTemplateSelector? ContentTemplateSelector
        {
            get { return GetValue(ContentTemplateSelectorProperty) as DataTemplateSelector; }
            set { SetValue(ContentTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ContentTemplateSelectorProperty"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentTemplateSelectorProperty = PropertyHelper.CreateProperty<DataTemplateSelector, NavigationSubView>(nameof(ContentTemplateSelector), null);

        /// <summary>
        /// Gets or sets the template selector for the header.
        /// </summary>
        public DataTemplateSelector? HeaderTemplateSelector
        {
            get { return GetValue(HeaderTemplateSelectorProperty) as DataTemplateSelector; }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="HeaderTemplateSelectorProperty"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty = PropertyHelper.CreateProperty<DataTemplateSelector, NavigationSubView>(nameof(HeaderTemplateSelector), null);

        /// <summary>
        /// Gets or sets the vertical scrollbar visibility of the scrollviewer below the title.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VerticalScrollBarVisibility"/> dependency property.
        /// </summary>
#if MAUI
        public static readonly BindableProperty VerticalScrollBarVisibilityProperty =
            BindableProperty.Create(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(NavigationSubView), ScrollBarVisibility.Default);
#else
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(NavigationSubView), new PropertyMetadata(ScrollBarVisibility.Auto));
#endif

    }
}