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
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationsFilterResultsPopupView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private CollectionView? _resultsListView;

        static UtilityAssociationsFilterResultsPopupView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Grid layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.AssociationsFilterResult?.Filter.Title, source: RelativeBindingSource.TemplatedParent);
            title.SetBinding(IsVisibleProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.IsExpanded, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            layout.Add(title);

            Label description = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            description.SetBinding(Label.TextProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.AssociationsFilterResult?.Filter.Description, source: RelativeBindingSource.TemplatedParent);
            description.SetBinding(IsVisibleProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.IsExpanded, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            description.Style = PopupViewer.GetPopupViewerCaptionStyle();
            Grid.SetRow(description, 1);
            layout.Add(description);

            Image icon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(5) };
            icon.Source = new FontImageSource() { Glyph = ToolkitIcons.ChevronRight, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            icon.SetBinding(IsVisibleProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.IsExpanded, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            Grid.SetRowSpan(icon, 2);
            Grid.SetColumn(icon, 1);
            layout.Add(icon);

            var divider = new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.LightGray, Margin = new Thickness(2) };
            divider.SetBinding(IsVisibleProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.IsExpanded, source: RelativeBindingSource.TemplatedParent, converter: InvertBoolConverter.Instance);
            Grid.SetRow(divider, 2);
            Grid.SetRowSpan(divider, 2);
            layout.Add(divider);

            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.AssociationsFilterResult?.GroupResults, source: RelativeBindingSource.TemplatedParent);
            cv.SetBinding(IsVisibleProperty, static (UtilityAssociationsFilterResultsPopupView view) => view.IsExpanded, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            Grid.SetRowSpan(cv, 3);
            Grid.SetColumnSpan(cv, 2);
            layout.Add(divider);

            INameScope nameScope = new NameScope();
            nameScope.RegisterName("ResultsList", cv);

            return layout;
        }

        private static object BuildDefaultItemTemplate()
        {
            var view = new UtilityAssociationGroupResultPopupView() { IsSearchable = false };
            view.SetBinding(UtilityAssociationGroupResultPopupView.GroupResultProperty, static (UtilityNetworks.UtilityAssociationGroupResult result) => result);

            return view;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("ResultsList") is CollectionView view)
            {
                _resultsListView = view;
            }
            UpdateView();
        }

        private void UpdateView()
        {
            if (_resultsListView is not null && AssociationsFilterResult is not null)
            {
                _resultsListView.ItemsSource = IsExpanded ? AssociationsFilterResult.GroupResults : Enumerable.Empty<UtilityNetworks.UtilityAssociationGroupResult>();
            }
        }
    }
}
#endif