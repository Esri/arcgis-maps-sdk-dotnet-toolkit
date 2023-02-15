using Esri.ArcGISRuntime.Mapping.Popups;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public class PopupMediaItemsControl : Control
    {
        private ButtonBase? _previousButton;
        private ButtonBase? _nextButton;
        private ContentPresenter? _contentPresenter;
        private int selectedIndex = 0;
        public PopupMediaItemsControl()
        {
            DefaultStyleKey= typeof(PopupMediaItemsControl);
        }

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

        public IEnumerable? ItemsSource
        {
            get { return GetValue(ItemsSourceProperty) as IEnumerable; }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(PopupMediaItemsControl), new PropertyMetadata(null, (s,e) => ((PopupMediaItemsControl)s).OnItemsSourcePropertyChanged()));

        int itemCount = 0;
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


        public DataTemplate PopupMediaImageTemplate
        {
            get { return (DataTemplate)GetValue(PopupMediaImageTemplateProperty); }
            set { SetValue(PopupMediaImageTemplateProperty, value); }
        }

        public static readonly DependencyProperty PopupMediaImageTemplateProperty =
            DependencyProperty.Register(nameof(PopupMediaImageTemplate), typeof(DataTemplate), typeof(PopupMediaItemsControl), new PropertyMetadata(null));

        public DataTemplate PopupMediaChartTemplate
        {
            get { return (DataTemplate)GetValue(PopupMediaChartTemplateProperty); }
            set { SetValue(PopupMediaChartTemplateProperty, value); }
        }

        public static readonly DependencyProperty PopupMediaChartTemplateProperty =
            DependencyProperty.Register(nameof(PopupMediaChartTemplate), typeof(DataTemplate), typeof(PopupMediaItemsControl), new PropertyMetadata(null));
    }
}
