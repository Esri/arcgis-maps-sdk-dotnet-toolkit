﻿// /*******************************************************************************
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
using System.Windows.Controls.Primitives;
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
        private int selectedIndex = 0;
#endif

        /// <inheritdoc />
#if WPF
        public override void OnApplyTemplate()
#elif WINDOWS_XAML
        protected override void OnApplyTemplate()
#endif
        {
#if WPF
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
            UpdateContent();
#elif WINUI
            CreatePipsPager();
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
        private void CreatePipsPager()
        {
            // Instead of creating Pips in XAML which UWP doesn't support, we create it in code here instead of having to maintain two sets of control templates
            if (GetTemplateChild("PipsPagerContainer") is ContentControl contentControl && GetTemplateChild("FlipView") is FlipView flipView)
            {
                PipsPager p = new PipsPager() { HorizontalAlignment = HorizontalAlignment.Center };
                p.SetBinding(PipsPager.NumberOfPagesProperty, new Binding() { Path = new PropertyPath("Element.Media.Count"), Source = this });
                p.SetBinding(PipsPager.SelectedPageIndexProperty, new Binding() { Path = new PropertyPath(nameof(FlipView.SelectedIndex)), Mode = BindingMode.TwoWay, Source = flipView });
                contentControl.Content = p;
            }
            UpdatePipsVisibility();
        }
        private void UpdatePipsVisibility()
        {
            if (GetTemplateChild("PipsPagerContainer") is ContentControl cc)
            {
                cc.Visibility = (Element?.Media?.Count ?? 0) > 1 ? Visibility.Visible : Visibility.Collapsed;
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