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

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationResultDetailsView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationResultDetailsView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            VerticalStackLayout layout = new VerticalStackLayout();

            Grid associationTypeGrid = new Grid() { Margin = new Thickness(0, 8, 0, 0) };
            associationTypeGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            associationTypeGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Label associationTypeLabel = new Label() { FontAttributes = FontAttributes.Bold, Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsAssociationType") };
            associationTypeGrid.Add(associationTypeLabel);
            Label associationTypeText = new Label() { HorizontalTextAlignment = TextAlignment.End };
            associationTypeText.SetBinding(Label.TextProperty, static (UtilityAssociationResultDetailsView view) => view.AssociationResult?.Association.AssociationType, source: RelativeBindingSource.TemplatedParent);
            Grid.SetColumn(associationTypeText, 1);
            associationTypeGrid.Add(associationTypeText);
            layout.Add(associationTypeGrid);

            Grid fromGrid = new Grid() { Margin = new Thickness(0, 8, 0, 0) };
            fromGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            fromGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            fromGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            fromGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            fromGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            fromGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Label fromElementLabel = new Label() { FontAttributes = FontAttributes.Bold, Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsFromElement") };
            fromGrid.Add(fromElementLabel);
            Label fromElementText = new Label() { HorizontalTextAlignment = TextAlignment.End };
            Grid.SetColumn(fromElementText, 1);
            fromGrid.Add(fromElementText);
            Label fromTerminalLabel = new Label() { FontAttributes = FontAttributes.Bold, Text = Properties.Resources.GetString("UtilityNetworkTraceToolTerminal"), IsVisible = false };
            Grid.SetRow(fromTerminalLabel, 1);
            fromGrid.Add(fromTerminalLabel);
            Label fromTerminalText = new Label() { HorizontalTextAlignment = TextAlignment.End };
            fromTerminalLabel.SetBinding(Label.IsVisibleProperty, static (Label view) => view.IsVisible, source: fromTerminalText);
            Grid.SetRow(fromTerminalText, 1);
            Grid.SetColumn(fromTerminalText, 1);
            fromGrid.Add(fromTerminalText);
            Label fromFractionLabel = new Label() { FontAttributes = FontAttributes.Bold, Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsPercentAlong"), IsVisible = false };
            Grid.SetRow(fromFractionLabel, 2);
            fromGrid.Add(fromFractionLabel);
            Slider fromFraction = new Slider() { Minimum = 0, Maximum = 1, IsEnabled = false, IsVisible = false };
            fromFractionLabel.SetBinding(Label.IsVisibleProperty, static (Slider view) => view.IsVisible, source: fromFraction);
            Grid.SetRow(fromFraction, 3);
            Grid.SetColumnSpan(fromFraction, 2);
            fromGrid.Add(fromFraction);
            layout.Add(fromGrid);

            Grid toGrid = new Grid() { Margin = new Thickness(0, 8, 0, 0) };
            toGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            toGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            toGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            toGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            toGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            toGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Label toElementLabel = new Label() { FontAttributes = FontAttributes.Bold, Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsToElement") };
            toGrid.Add(toElementLabel);
            Label toElementText = new Label() { HorizontalTextAlignment = TextAlignment.End };
            Grid.SetColumn(toElementText, 1);
            toGrid.Add(toElementText);
            Label toTerminalLabel = new Label() { FontAttributes = FontAttributes.Bold, Text = Properties.Resources.GetString("UtilityNetworkTraceToolTerminal"), IsVisible = false };
            Grid.SetRow(toTerminalLabel, 1);
            toGrid.Add(toTerminalLabel);
            Label toTerminalText = new Label() { HorizontalTextAlignment = TextAlignment.End };
            toTerminalLabel.SetBinding(Label.IsVisibleProperty, static (Label view) => view.IsVisible, source: toTerminalText);
            Grid.SetRow(toTerminalText, 1);
            Grid.SetColumn(toTerminalText, 1);
            toGrid.Add(toTerminalText);
            Label toFractionLabel = new Label() { FontAttributes = FontAttributes.Bold, Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsPercentAlong"), IsVisible = false };
            Grid.SetRow(toFractionLabel, 2);
            toGrid.Add(toFractionLabel);
            Slider toFraction = new Slider() { Minimum = 0, Maximum = 1, IsEnabled = false, IsVisible = false };
            toFractionLabel.SetBinding(Label.IsVisibleProperty, static (Slider view) => view.IsVisible, source: toFraction);
            Grid.SetRow(toFraction, 3);
            Grid.SetColumnSpan(toFraction, 2);
            toGrid.Add(toFraction);
            layout.Add(toGrid);

            Button removeAssociationButton = new Button() { Margin = new Thickness(0, 8, 0, 0), Text = Properties.Resources.GetString("FeatureFormUtilityAssociationsRemoveAssociation") };
            layout.Add(removeAssociationButton);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(layout, nameScope);
            nameScope.RegisterName("FromElementText", fromElementText);
            nameScope.RegisterName("ToElementText", toElementText);
            nameScope.RegisterName("FromTerminalText", fromTerminalText);
            nameScope.RegisterName("ToTerminalText", toTerminalText);
            nameScope.RegisterName("FromFraction", fromFraction);
            nameScope.RegisterName("ToFraction", toFraction);
            nameScope.RegisterName("RemoveAssociationButton", removeAssociationButton);

            return layout;
        }
    }
}
#endif