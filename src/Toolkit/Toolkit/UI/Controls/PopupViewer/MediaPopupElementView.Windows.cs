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
#if WPF || WINDOWS_XAML
using Esri.ArcGISRuntime.Mapping.Popups;
using System.Collections;
#if WPF
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
#elif WINUI
using Microsoft.UI.Xaml.Automation.Peers;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="MediaPopupElement"/>.
    /// </summary>
    [TemplatePart(Name = "PreviousButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "NextButton", Type = typeof(ButtonBase))]
    public partial class MediaPopupElementView : Control
    {
#if WPF
        private ButtonBase? _previousButton;
        private ButtonBase? _nextButton;
        private FrameworkElement? _currentMediaView;
        private int selectedIndex = 0;
#endif

        // A plain Control has no automation peer by default. Without this override, the enclosing
        // PopupElementItemsControl's ItemsControlAutomationPeer can't find a real peer for this item and falls
        // back to a synthetic ItemAutomationPeer wrapping the raw MediaPopupElement data object - which hides
        // this view's real content (title, caption, prev/next buttons) from Narrator's navigation entirely.
#if WPF
        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
#elif WINUI
        /// <inheritdoc />
        protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
#endif

        /// <inheritdoc />
#if WPF
        public override void OnApplyTemplate()
#elif WINDOWS_XAML
        protected override void OnApplyTemplate()
#endif
        {
#if WPF
            PreviewKeyDown -= OnPreviewKeyDown;
            PreviewKeyDown += OnPreviewKeyDown;

            if (_previousButton != null)
            {
                _previousButton.Click -= OnPreviousButtonClicked;
            }
            if (_nextButton != null)
            {
                _nextButton.Click -= OnNextButtonClicked;
            }
            _previousButton = GetTemplateChild("PreviousButton") as ButtonBase;
            if (_previousButton != null)
            {
                _previousButton.Click += OnPreviousButtonClicked;
            }
            _nextButton = GetTemplateChild("NextButton") as ButtonBase;
            if (_nextButton != null)
            {
                _nextButton.Click += OnNextButtonClicked;
            }
            _currentMediaView = GetTemplateChild("CurrentMediaView") as FrameworkElement;
            UpdateContent();
#elif WINUI
            UpdatePipsVisibility();
#endif
            base.OnApplyTemplate();
        }

#if WPF
        /// <summary>
        /// Gets or sets the currently display <see cref="PopupMedia"/>.
        /// </summary>
        public PopupMedia? CurrentItem
        {
            get => GetValue(CurrentItemProperty) as PopupMedia;
            set => SetValue(CurrentItemProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CurrentItem"/> dependency property.
        /// </summary>       
        public static readonly DependencyProperty CurrentItemProperty =
            DependencyProperty.Register(nameof(CurrentItem), typeof(PopupMedia), typeof(MediaPopupElementView), new PropertyMetadata(null));

        private void UpdateContent()
        {
            var itemCount = Element?.Media?.Count ?? 0;
            if (_previousButton != null)
                _previousButton.Visibility = itemCount < 2 ? Visibility.Collapsed : Visibility.Visible;
            if (_nextButton != null)
                _nextButton.Visibility = itemCount < 2 ? Visibility.Collapsed : Visibility.Visible;
            PopupMedia? content = null;
            if (Element?.Media != null)
            {
                if (selectedIndex >= 0 && selectedIndex < itemCount)
                {
                    content = Element.Media[selectedIndex];
                }
            }
            CurrentItem = content;

            // Narrator announces "item X of Y" on its own from these two properties - without them the
            // currently-shown media item gives no indication of its position among the others.
            if (_currentMediaView != null)
            {
                System.Windows.Automation.AutomationProperties.SetPositionInSet(_currentMediaView, selectedIndex + 1);
                System.Windows.Automation.AutomationProperties.SetSizeOfSet(_currentMediaView, itemCount);
            }
        }

        private void OnPreviousButtonClicked(object sender, RoutedEventArgs e)
        {
            selectedIndex--;
            if (selectedIndex < 0)
            {
                selectedIndex = (Element?.Media?.Count ?? 1) - 1;
            }
            UpdateContent();
        }

        private void OnNextButtonClicked(object sender, RoutedEventArgs e)
        {
            selectedIndex++;
            if (selectedIndex >= (Element?.Media?.Count ?? 0))
            {
                selectedIndex = 0;
            }
            UpdateContent();
        }

        // Lets the Left/Right arrow keys page through media items while focus is anywhere inside this control
        // (e.g. on the prev/next buttons themselves), in addition to clicking them. PreviewKeyDown is a
        // tunneling event, so wiring it on this control - rather than the individual buttons - catches the key
        // regardless of which descendant currently has focus. Only handled when there's more than one item, so
        // arrow keys don't do anything surprising (or swallow the keystroke) when paging isn't possible.
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Element?.Media?.Count ?? 0) < 2)
            {
                return;
            }

            if (e.Key == Key.Left)
            {
                OnPreviousButtonClicked(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                OnNextButtonClicked(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

#endif
        private void OnElementPropertyChanged()
        {
#if WPF
            selectedIndex = 0;
            UpdateContent();
#elif WinUI
            UpdatePipsVisibility();
#endif
        }

#if WINUI
        private void UpdatePipsVisibility()
        {
            if (GetTemplateChild("PipsPager") is UIElement pager)
            {
                pager.Visibility = (Element?.Media?.Count ?? 0) > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
#endif

        /// <summary>
        /// Gets or sets the template for popup media items.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(MediaPopupElementView), new PropertyMetadata(null));
    }
}
#endif