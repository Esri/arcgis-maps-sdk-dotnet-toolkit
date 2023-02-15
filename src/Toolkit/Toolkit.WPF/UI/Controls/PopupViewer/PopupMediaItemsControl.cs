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

using Esri.ArcGISRuntime.Mapping.Popups;
using System.Collections;
using System.Windows.Controls.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting class for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a collection of <see cref="PopupMedia"/> in a flip-view style control.
    /// </summary>
    public class PopupMediaItemsControl : Control
    {
        private ButtonBase? _previousButton;
        private ButtonBase? _nextButton;
        private ContentPresenter? _contentPresenter;
        private int selectedIndex = 0;
        private int itemCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupMediaItemsControl"/> class.
        /// </summary>
        public PopupMediaItemsControl()
        {
            DefaultStyleKey= typeof(PopupMediaItemsControl);
        }
        
        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_previousButton != null)
            {
                _previousButton.Click -= OnPreviousButtonClicked;
            }
            if (_nextButton != null)
            {
                _nextButton.Click -= OnNextButtonClicked;
            }
            if (_contentPresenter != null)
            {
                _contentPresenter.Content = null;
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
            _contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;
            UpdateContent();
            base.OnApplyTemplate();
        }

        private void UpdateContent()
        {
            if (_previousButton != null)
                _previousButton.Visibility = itemCount == 0 || selectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (_nextButton != null)
                _nextButton.Visibility = itemCount == 0 || selectedIndex == itemCount - 1 ? Visibility.Collapsed : Visibility.Visible;
            if (_contentPresenter != null)
            {
                object? content = null;
                if (itemCount > 0 && ItemsSource != null)
                {
                    var enumerator = ItemsSource.GetEnumerator();
                    for (int i = 0; i <= selectedIndex && enumerator.MoveNext(); i++)
                    {
                        content = enumerator.Current;
                    }
                }
                DataTemplate? template = null;
                if (content is PopupMedia m)
                {
                    if (m.Type == PopupMediaType.Image)
                    {
                        template = PopupMediaImageTemplate;
                    }
                    else // Chart
                    {
                        template = PopupMediaChartTemplate;
                    }
                }
                _contentPresenter.ContentTemplate = template;
                _contentPresenter.Content = content;
            }
        }

        private void OnPreviousButtonClicked(object sender, RoutedEventArgs e)
        {
            if (selectedIndex > 0)
            {
                selectedIndex--;
                UpdateContent();
            }
        }

        private void OnNextButtonClicked(object sender, RoutedEventArgs e)
        {
            if (selectedIndex + 1< itemCount)
            {
                selectedIndex++;
                UpdateContent();
            }
        }

        /// <summary>
        /// Gets or sets a collection used to generate the content of the <see cref="PopupMediaItemsControl"/>.
        /// This must be a collection of <see cref="PopupMedia"/>.
        /// </summary>
        public IEnumerable? ItemsSource
        {
            get { return GetValue(ItemsSourceProperty) as IEnumerable; }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemsSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(PopupMediaItemsControl), new PropertyMetadata(null, (s,e) => ((PopupMediaItemsControl)s).OnItemsSourcePropertyChanged()));

        private void OnItemsSourcePropertyChanged()
        {
            itemCount = 0;
            selectedIndex = 0;
            if (ItemsSource is ICollection coll)
                itemCount = coll.Count;
            else if (ItemsSource is IEnumerable e)
            {
                var enumerator = e.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    itemCount++;
                }
            }
            UpdateContent();
        }

        /// <summary>
        /// Gets or sets the template for Image media.
        /// </summary>
        public DataTemplate PopupMediaImageTemplate
        {
            get { return (DataTemplate)GetValue(PopupMediaImageTemplateProperty); }
            set { SetValue(PopupMediaImageTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PopupMediaImageTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PopupMediaImageTemplateProperty =
            DependencyProperty.Register(nameof(PopupMediaImageTemplate), typeof(DataTemplate), typeof(PopupMediaItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template for Chart media.
        /// </summary>
        public DataTemplate PopupMediaChartTemplate
        {
            get { return (DataTemplate)GetValue(PopupMediaChartTemplateProperty); }
            set { SetValue(PopupMediaChartTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PopupMediaChartTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PopupMediaChartTemplateProperty =
            DependencyProperty.Register(nameof(PopupMediaChartTemplate), typeof(DataTemplate), typeof(PopupMediaItemsControl), new PropertyMetadata(null));
    }
}
