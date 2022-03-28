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

#if IsWPF

using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Esri.ArcGISRuntime.Mapping.Floor;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "PART_Placeholder", Type=typeof(UIElement))]
    [TemplatePart(Name = "PART_ClearButton", Type=typeof(Button))]
    [TemplatePart(Name = "PART_NoResultsLabel", Type = typeof(UIElement))]
    internal class FilteringListView : ListView
    {
        private UIElement? _placeholderView;
        private UIElement? _noResultsLabel;
        private Button? _clearButton;
        private bool _waitFlag;
        private SelectionChangedEventArgs? _lastSelectionChangedArgs;

        public object? RememberingSelectedItem
        {
            get => GetValue(RememberingSelectedItemProperty);
            set => SetValue(RememberingSelectedItemProperty, value);
        }

        public static readonly DependencyProperty RememberingSelectedItemProperty =
            DependencyProperty.Register(nameof(RememberingSelectedItem), typeof(object), typeof(FilteringListView), new PropertyMetadata(null, OnRememberingSelectedItemChanged));

        private static void OnRememberingSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is FilteringListView sendingView)
            {
                sendingView.SelectedItem = args.NewValue;
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (ItemsSource == null)
            {
                SetValue(HasSearchCapabilityKey, false);
            }
            else
            {
                SetValue(HasSearchCapabilityKey, ItemsSource.OfType<object>().Count() > 1);
            }

            if (SelectedItem == null && RememberingSelectedItem != null && Items.Contains(RememberingSelectedItem))
            {
                SelectedItem = RememberingSelectedItem;
            }

            if (Items.Count == 0)
            {
                _noResultsLabel?.SetValue(VisibilityProperty, Visibility.Visible);
            }
            else
            {
                _noResultsLabel?.SetValue(VisibilityProperty, Visibility.Collapsed);
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (SelectedItem != null && (Items?.Contains(SelectedItem) ?? false))
            {
                ScrollIntoView(SelectedItem);
            }

            if (SelectedItem != null && RememberingSelectedItem != SelectedItem)
            {
                RememberingSelectedItem = SelectedItem;
                _lastSelectionChangedArgs = e;
                base.OnSelectionChanged(e);
            }
        }

        public int TypingDelayMilliseconds
        {
            get => (int)GetValue(TypingDelayMillisecondsProperty);
            set => SetValue(TypingDelayMillisecondsProperty, value);
        }

        internal static readonly DependencyPropertyKey HasSearchCapabilityKey =
        DependencyProperty.RegisterReadOnly(nameof(HasSearchCapability), typeof(bool), typeof(FilteringListView), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool HasSearchCapability =>
            (bool)GetValue(HasSearchCapabilityKey.DependencyProperty);

        public static readonly DependencyProperty TypingDelayMillisecondsProperty =
            DependencyProperty.Register(nameof(TypingDelayMilliseconds), typeof(int), typeof(FilteringListView), new PropertyMetadata(100));

        public string? Placeholder
        {
            get => GetValue(PlaceholderProperty) as string;
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(FilteringListView), null);

        public string? SearchString
        {
            get => GetValue(SearchStringProperty) as string;
            set => SetValue(SearchStringProperty, value);
        }

        public static readonly DependencyProperty SearchStringProperty =
            DependencyProperty.Register(nameof(SearchString), typeof(string), typeof(FilteringListView), new PropertyMetadata(string.Empty, HandleSearchChanged));

        private static void HandleSearchChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is FilteringListView sendingView)
            {
                sendingView.HandleTextChanged();
            }
        }

        public string? NoResultsMessage
        {
            get => GetValue(NoResultsMessageProperty) as string;
            set => SetValue(NoResultsMessageProperty, value);
        }

        public static readonly DependencyProperty NoResultsMessageProperty =
            DependencyProperty.Register(nameof(NoResultsMessage), typeof(string), typeof(FilteringListView), null);

        private async void HandleTextChanged()
        {
            if (string.IsNullOrWhiteSpace(SearchString))
            {
                _placeholderView?.SetValue(VisibilityProperty, Visibility.Visible);
                _clearButton?.SetValue(VisibilityProperty, Visibility.Collapsed);
            }
            else
            {
                _placeholderView?.SetValue(VisibilityProperty, Visibility.Collapsed);
                _clearButton?.SetValue(VisibilityProperty, Visibility.Visible);
            }

            if (_waitFlag)
            {
                return;
            }

            _waitFlag = true;
            await Task.Delay(TypingDelayMilliseconds);
            _waitFlag = false;

            CollectionViewSource.GetDefaultView(ItemsSource).Filter = (item) =>
            {
                return AsFilterString(item).Contains(SearchString?.ToLower() ?? string.Empty);
            };
        }

        static FilteringListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FilteringListView), new FrameworkPropertyMetadata(typeof(FilteringListView)));
        }

        public override void OnApplyTemplate()
        {
            if (_clearButton != null)
            {
                _clearButton.Click -= HandleClearButtonClick;
                _clearButton = null;
            }

            base.OnApplyTemplate();

            _placeholderView = GetTemplateChild("PART_Placeholder") as UIElement;
            _noResultsLabel = GetTemplateChild("PART_NoResultsLabel") as UIElement;

            if (GetTemplateChild("PART_ClearButton") is Button clearButton)
            {
                _clearButton = clearButton;
                _clearButton.Click += HandleClearButtonClick;
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            // Listens for clicks and if the clicked item matches the selected item,
            // re-raises the selection event; allows listeners to handle re-selection of item.
            base.OnPreviewMouseUp(e);
            if (e.OriginalSource is FrameworkElement element && element.DataContext == SelectedItem && _lastSelectionChangedArgs != null)
            {
                base.OnSelectionChanged(_lastSelectionChangedArgs);
            }
        }

        private void HandleClearButtonClick(object sender, RoutedEventArgs e) => SearchString = null;

        private static string AsFilterString(object input)
        {
            if (input is string inputString)
            {
                return inputString;
            }
            else if (input is FloorFacility facilityInput)
            {
                return facilityInput.Name.ToLower();
            }
            else if (input is FloorSite siteInput)
            {
                return siteInput.Name.ToLower();
            }
            else
            {
                return input?.ToString() ?? string.Empty;
            }
        }
    }
}

#endif