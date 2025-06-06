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
using System.Globalization;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    public partial class UtilityAssociationResultView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationResultView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
           Grid layout = new Grid() { Padding = new Thickness(8, 0, 8, 0), MinimumHeightRequest = 40 };
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Image icon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center };
            Grid.SetRowSpan(icon, 2);
            layout.Add(icon);

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityAssociationResultView result) => result.AssociationResult?.Title, source: RelativeBindingSource.TemplatedParent);
            title.Style = FeatureFormView.GetFeatureFormTitleStyle();
            Grid.SetColumn(title, 1);
            Grid.SetColumnSpan(title, 3);
            layout.Add(title);

            Label fractionText = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false };
            Grid.SetRow(fractionText, 1);
            Grid.SetColumn(fractionText, 1);
            layout.Add(fractionText);

            Label isContentVisibleText = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false };
            Grid.SetRow(isContentVisibleText, 1);
            Grid.SetColumn(isContentVisibleText, 2);
            layout.Add(isContentVisibleText);

            Label terminalText = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false };
            Grid.SetRow(terminalText, 1);
            Grid.SetColumn(terminalText, 3);
            layout.Add(terminalText);

            Image image = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center };
            image.Source = new FontImageSource() { Glyph = ((char)0xE7A0).ToString(), Color = Colors.Gray, FontFamily = "toolkit-icons", Size = 18 };
            Grid.SetColumn(image, 4);
            Grid.SetRowSpan(image, 2);
            layout.Add(image);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(layout, nameScope);
            nameScope.RegisterName("Icon", icon);
            nameScope.RegisterName("FractionText", fractionText);
            nameScope.RegisterName("IsContentVisibleText", isContentVisibleText);
            nameScope.RegisterName("TerminalText", terminalText);
            return layout;
        }
    }
}
#endif