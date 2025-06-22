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
           Grid layout = new Grid() { MinimumHeightRequest = 40 };
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Image icon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0,0,4,0) };
            Grid.SetRowSpan(icon, 2);
            layout.Add(icon);

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityAssociationResultView result) => result.AssociationResult?.Title, source: RelativeBindingSource.TemplatedParent);
            title.Style = FeatureFormView.GetFeatureFormTitleStyle();
            Grid.SetColumn(title, 1);
            layout.Add(title);

            Label connectionInfo = new Label() { Style = FeatureFormView.GetFeatureFormCaptionStyle(), IsVisible = false, LineBreakMode = LineBreakMode.TailTruncation, Margin = new Thickness(0,0,2,0) };
            connectionInfo.SetBinding(ToolTipProperties.TextProperty, static (Label label) => label.Text, source: RelativeBindingSource.Self);
            Grid.SetRow(connectionInfo, 1);
            Grid.SetColumn(connectionInfo, 1);
            layout.Add(connectionInfo);

            Image image = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center };
            image.Source = new FontImageSource() { Glyph = ToolkitIcons.ChevronRight, Color = Colors.Gray, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            Grid.SetColumn(image, 2);
            Grid.SetRowSpan(image, 2);
            layout.Add(image);
            // TODO: Set theme-based background once https://github.com/dotnet/maui/issues/26620 is addressed
            // var g = new VisualStateGroup();
            // g.States.Add(new VisualState() { Name = "Normal" });
            // g.States.Add(new VisualState() { Name = "PointerOver" });
            // g.States[1].Setters.Add(new Setter() { Property = Grid.BackgroundColorProperty, Value = Colors.LightGray });
            // VisualStateManager.SetVisualStateGroups(layout, new VisualStateGroupList { g });

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(layout, nameScope);
            nameScope.RegisterName("Icon", icon);
            nameScope.RegisterName("ConnectionInfo", connectionInfo);
            return layout;
        }
    }
}
#endif