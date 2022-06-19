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
#if WINDOWS && !WINDOWS_UWP
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Special listview that facilitates inspecting individual items. Created for use with <see cref="UtilityNetworkTraceTool"/>.
    /// </summary>
    [TemplatePart(Name = "PART_InnerListView", Type = typeof(ListView))]
    internal class StartingPointListView : ListView
    {
        private ListView? _innerListView;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartingPointListView"/> class.
        /// </summary>
        public StartingPointListView()
        {
            DefaultStyleKey = typeof(StartingPointListView);
            CloseInspectorCommand = new DelegateCommand(obj =>
            {
                IsInspecting = false;
                SelectedItem = null;
            });
            OpenInspectorCommand = new DelegateCommand(obj =>
            {
                SelectedItem = obj;
                IsInspecting = true;
            });
            SelectNextItemCommand = new DelegateCommand(obj => SelectNextItem());
            SelectPreviousItemCommand = new DelegateCommand(obj => SelectPreviousItem());
            DeleteSelectedCommand = new DelegateCommand(obj => DeleteSelected(obj));
            UpdateState();
        }

#if WINDOWS_UWP
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_InnerListView") is ListView innerListView)
            {
                _innerListView = innerListView;
                _innerListView.SelectionChanged += InnerListView_HandleSelectionChanged;
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            UpdateState();
        }

        private void InnerListView_HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Keep selection in sync with internal list view.
            foreach (var item in e.AddedItems)
            {
                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }

            foreach (var item in e.RemovedItems)
            {
                if (SelectedItems.Contains(item))
                {
                    SelectedItems.Remove(item);
                }
            }
        }

        private void SelectNextItem()
        {
            if (SelectedItems.Count == 1)
            {
                SelectedIndex = (SelectedIndex + 1) % Items.Count;
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // Keep internal list view selection in sync with selection.
            if (_innerListView?.SelectedItems is IList selectedItems)
            {
                foreach (var item in e.AddedItems)
                {
                    if (!selectedItems.Contains(item))
                    {
                        selectedItems.Add(item);
                    }
                }

                foreach (var item in e.RemovedItems)
                {
                    if (selectedItems.Contains(item))
                    {
                        selectedItems.Remove(item);
                    }
                }
            }

            UpdateState();
        }

        private void UpdateState()
        {
            InspectorViewSelectionLabelText = $"{SelectedIndex + 1}/{Items.Count}";
            if (SelectedItems.Count < 1)
            {
                IsInspecting = false;
            }

            (CloseInspectorCommand as DelegateCommand)?.NotifyCanExecuteChanged(IsInspecting);
            (SelectNextItemCommand as DelegateCommand)?.NotifyCanExecuteChanged(Items.Count > 1);
            (SelectPreviousItemCommand as DelegateCommand)?.NotifyCanExecuteChanged(Items.Count > 1);
        }

        private void SelectPreviousItem()
        {
            if (SelectedItems.Count == 1)
            {
                var newIndex = (SelectedIndex - 1) % Items.Count;
                SelectedIndex = newIndex < 0 ? newIndex + Items.Count : newIndex;
            }
        }

        private void DeleteSelected(object? parameter)
        {
            var toDelete = parameter ?? SelectedItem;
            if (toDelete != null && ItemsSource is IList modifiable)
            {
                modifiable.Remove(toDelete);
            }
        }

        /// <summary>
        /// Gets or sets the template to use when inspecting a specific item.
        /// </summary>
        public DataTemplate? InspectorViewTemplate
        {
            get => GetValue(InspectorViewTemplateProperty) as DataTemplate;
            set => SetValue(InspectorViewTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="InspectorViewTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InspectorViewTemplateProperty =
            DependencyProperty.Register(nameof(InspectorViewTemplate), typeof(DataTemplate), typeof(StartingPointListView), null);

        /// <summary>
        /// Gets or sets a value indicating whether the selected item is being inspected.
        /// </summary>
        public bool IsInspecting
        {
            get => (bool)GetValue(IsInspectingProperty);
            set => SetValue(IsInspectingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsInspecting"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsInspectingProperty =
            DependencyProperty.Register(nameof(IsInspecting), typeof(bool), typeof(StartingPointListView), new PropertyMetadata(false, OnIsInspectingChanged));

        private static void OnIsInspectingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as StartingPointListView)?.UpdateState();
        }

        /// <summary>
        /// Gets or sets the command used to close the inspection view.
        /// </summary>
        public ICommand? CloseInspectorCommand
        {
            get => GetValue(CloseInspectorCommandProperty) as ICommand;
            set => SetValue(CloseInspectorCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseInspectorCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseInspectorCommandProperty =
            DependencyProperty.Register(nameof(CloseInspectorCommand), typeof(ICommand), typeof(StartingPointListView), null);

        /// <summary>
        /// Gets or sets the command used to open the inspection view.
        /// </summary>
        public ICommand? OpenInspectorCommand
        {
            get => GetValue(OpenInspectorCommandProperty) as ICommand;
            set => SetValue(OpenInspectorCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OpenInspectorCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OpenInspectorCommandProperty =
            DependencyProperty.Register(nameof(OpenInspectorCommand), typeof(ICommand), typeof(StartingPointListView), null);

        /// <summary>
        /// Gets or sets the command used to select the next item in the list.
        /// </summary>
        public ICommand? SelectNextItemCommand
        {
            get => GetValue(SelectNextItemCommandProperty) as ICommand;
            set => SetValue(SelectNextItemCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectNextItemCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectNextItemCommandProperty =
            DependencyProperty.Register(nameof(SelectNextItemCommand), typeof(ICommand), typeof(StartingPointListView), null);

        /// <summary>
        /// Gets or sets the command used to select the previous item in the list.
        /// </summary>
        public ICommand? SelectPreviousItemCommand
        {
            get => GetValue(SelectPreviousItemCommandProperty) as ICommand;
            set => SetValue(SelectPreviousItemCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectPreviousItemCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectPreviousItemCommandProperty =
            DependencyProperty.Register(nameof(SelectPreviousItemCommand), typeof(ICommand), typeof(StartingPointListView), null);

        /// <summary>
        /// Gets or sets the command used to delete the selected item.
        /// </summary>
        public ICommand? DeleteSelectedCommand
        {
            get => GetValue(DeleteSelectedCommandProperty) as ICommand;
            set => SetValue(DeleteSelectedCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DeleteSelectedCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DeleteSelectedCommandProperty =
            DependencyProperty.Register(nameof(DeleteSelectedCommand), typeof(ICommand), typeof(StartingPointListView), null);

        /// <summary>
        /// Gets or sets the label that identifies the selected item.
        /// </summary>
        public string? InspectorViewSelectionLabelText
        {
            get => GetValue(InspectorViewSelectionLabelTextProperty) as string;
            set => SetValue(InspectorViewSelectionLabelTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="InspectorViewSelectionLabelText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InspectorViewSelectionLabelTextProperty =
            DependencyProperty.Register(nameof(InspectorViewSelectionLabelText), typeof(string), typeof(StartingPointListView), null);

        /// <summary>
        /// Gets or sets the command used to zoom to a specific item.
        /// </summary>
        public ICommand? ZoomToCommand
        {
            get => GetValue(ZoomToCommandProperty) as ICommand;
            set => SetValue(ZoomToCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ZoomToCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomToCommandProperty =
            DependencyProperty.Register(nameof(ZoomToCommand), typeof(ICommand), typeof(StartingPointListView), null);
    }
}
#endif