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
using Esri.ArcGISRuntime.Mapping.FeatureForms;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationsFormElementView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationsFormElementView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
           VerticalStackLayout root = new VerticalStackLayout();
            Label title = new Label();
            title.SetBinding(Label.TextProperty, static (UtilityAssociationsFormElementView view) => view.Element?.Label, source: RelativeBindingSource.TemplatedParent);
            title.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationsFormElementView view) => view.Element?.Label, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = FeatureFormView.GetFeatureFormTitleStyle();
            root.Add(title);
            Label description = new Label();
            description.SetBinding(Label.TextProperty, static (UtilityAssociationsFormElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent);
            description.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationsFormElementView view) => view.Element?.Description, source: RelativeBindingSource.TemplatedParent, converter: Internal.EmptyToFalseConverter.Instance);
            description.Style = FeatureFormView.GetFeatureFormCaptionStyle();
            root.Add(description);
            root.Add(new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.Gray, Margin = new Thickness(0, 5) });
            CollectionView cv = new CollectionView() { SelectionMode = SelectionMode.None };
            cv.SetBinding(CollectionView.ItemsSourceProperty, static (UtilityAssociationsFormElementView view) => view.Element?.AssociationsFilterResults, source: RelativeBindingSource.TemplatedParent);
            cv.ItemTemplate = new DataTemplate(BuildDefaultItemTemplate);
            root.Add(cv);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("AssociationsList", cv);
            return root;
        }

        private static object BuildDefaultItemTemplate()
        {
            Grid layout = new Grid() { Padding = new Thickness(10, 0, 0, 0) };
            TapGestureRecognizer itemTapGesture = new TapGestureRecognizer();
            itemTapGesture.Tapped += Result_Tapped;
            layout.GestureRecognizers.Add(itemTapGesture);
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(20));
            layout.RowDefinitions.Add(new RowDefinition(20));

            Label title = new Label();
            title.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Title);
            title.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Title, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = FeatureFormView.GetFeatureFormTitleStyle();
            layout.Add(title);
            Label description = new Label();
            description.SetBinding(Label.TextProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Description);
            description.SetBinding(VisualElement.IsVisibleProperty, static (UtilityNetworks.UtilityAssociationsFilterResult result) => result.Filter?.Description, converter: Internal.EmptyToFalseConverter.Instance);
            description.Style = FeatureFormView.GetFeatureFormCaptionStyle();

            Grid.SetRow(description, 1);
            layout.Add(description);
            Image image = new Image() { WidthRequest = 18, HeightRequest = 18 };
            image.Source = new FontImageSource() { Glyph = ((char)0xE078).ToString(), Color = Colors.Gray, FontFamily = "calcite-ui-icons-24", Size = 18 };
            Grid.SetColumn(image, 1);
            Grid.SetRowSpan(image, 2);
            layout.Add(image);

            Border root = new Border() { StrokeThickness = 0, Content = layout };
            return root;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        private FeatureFormView? GetFeatureFormViewParent()
        {
            var parent = this.Parent;
            while (parent is not null && parent is not FeatureFormView)
            {
                parent = parent.Parent;
            }
            return parent as FeatureFormView;
        }

        private static void Result_Tapped(object? sender, EventArgs e)
        {
            var cell = sender as View;
            Element? parent = cell?.Parent;
            while (parent is View && parent is not UtilityAssociationsFormElementView)
            {
                parent = parent.Parent;
            }
            if (parent is UtilityAssociationsFormElementView a && cell?.BindingContext is UtilityNetworks.UtilityAssociationsFilterResult result)
            {
                // TODO
                //a.FeatureFormView()?.NavigateToItem(result);
            }
        }
    }
}
#endif