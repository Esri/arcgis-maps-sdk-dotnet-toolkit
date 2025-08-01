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

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    [TemplatePart(Name = "ResultsList", Type = typeof(ItemsControl))]
    public partial class UtilityAssociationsFilterResultsPopupView : Control
    {
        private ItemsControl? _resultsItemsControl;
        /// <inheritdoc />
#if WINDOWS_XAML
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("ResultsList") is ItemsControl control)
            {
                _resultsItemsControl = control;
            }
        }

        /// <summary>
        /// Gets or sets the template for UtilityAssociationsFilterResult items.
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
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(UtilityAssociationsFilterResultsPopupView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the display count from <see cref="Mapping.Popups.UtilityAssociationsPopupElement.DisplayCount"/>.
        /// </summary>
        public int DisplayCount
        {
            get => (int)GetValue(DisplayCountProperty);
            set => SetValue(DisplayCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DisplayCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayCountProperty =
            DependencyProperty.Register(nameof(DisplayCount), typeof(int), typeof(UtilityAssociationsFilterResultsPopupView), new PropertyMetadata(1));

        /// <summary>
        /// Gets or sets a value indicating whether results in this filter is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsExpanded"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(UtilityAssociationsFilterResultsPopupView), new PropertyMetadata(false));

        private void UpdateView()
        {
            if (_resultsItemsControl is not null && AssociationsFilterResult is not null)
            {
                _resultsItemsControl.ItemsSource = IsExpanded ? AssociationsFilterResult.GroupResults : Enumerable.Empty<UtilityNetworks.UtilityAssociationGroupResult>();
            }
        }

        private void OnAssociationsFilterResultPropertyChanged()
        {
            UpdateView();
        }
    }
}
#endif