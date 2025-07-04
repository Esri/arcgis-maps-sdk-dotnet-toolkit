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
    public partial class UtilityAssociationResultPopupView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityAssociationResultPopupView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Grid layout = new Grid() { Padding = new Thickness(10, 0, 0, 0) };
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Image icon = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(5) };
            Grid.SetRowSpan(icon, 2);
            layout.Add(icon);

            Label title = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            title.SetBinding(Label.TextProperty, static (UtilityAssociationResultPopupView result) => result.AssociationResult?.Title, source: RelativeBindingSource.TemplatedParent);
            title.SetBinding(VisualElement.IsVisibleProperty, static (UtilityAssociationResultPopupView result) => result.AssociationResult?.Title, converter: Internal.EmptyToFalseConverter.Instance);
            title.Style = PopupViewer.GetPopupViewerTitleStyle();
            Grid.SetColumn(title, 1);
            layout.Add(title);

            Label fractionAlong = new Label() { VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            Grid.SetColumn(fractionAlong, 2);
            layout.Add(fractionAlong);

            Label connectionInfo = new Label() { Style = PopupViewer.GetPopupViewerCaptionStyle(), IsVisible = false, LineBreakMode = LineBreakMode.TailTruncation, Margin = new Thickness(0, 0, 2, 0) };
            connectionInfo.SetBinding(ToolTipProperties.TextProperty, static (Label label) => label.Text, source: RelativeBindingSource.Self);
            Grid.SetRow(connectionInfo, 1);
            Grid.SetColumn(connectionInfo, 1);
            layout.Add(connectionInfo);

            Image image = new Image() { WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center };
            image.Source = new FontImageSource() { Glyph = ToolkitIcons.ChevronRight, Color = Colors.Gray, FontFamily = ToolkitIcons.FontFamilyName, Size = 18 };
            Grid.SetColumn(image, 3);
            Grid.SetRowSpan(image, 2);
            layout.Add(image);

            var divider = new Border() { StrokeThickness = 0, HeightRequest = 1, BackgroundColor = Colors.LightGray, Margin = new Thickness(2) };
            Grid.SetRow(divider, 2);
            Grid.SetColumnSpan(divider, 4);
            layout.Add(divider);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(layout, nameScope);
            nameScope.RegisterName("FractionAlong", fractionAlong);
            nameScope.RegisterName("Icon", icon);
            nameScope.RegisterName("ConnectionInfo", connectionInfo);
            return layout;
        }
    }
}
#endif