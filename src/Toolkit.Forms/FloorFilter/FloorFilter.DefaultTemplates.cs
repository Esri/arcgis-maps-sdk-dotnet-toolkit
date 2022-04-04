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

using Xamarin.Forms;
using Grid = Xamarin.Forms.Grid;
using XForms = Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    public partial class FloorFilter
    {
        private static readonly DataTemplate DefaultLevelDataTemplate;
        private static readonly DataTemplate DefaultFacilityDataTemplate;
        private static readonly DataTemplate DefaultSiteDataTemplate;
        private static readonly DataTemplate DefaultDifferentiatingFacilityDataTemplate;
        private static readonly ControlTemplate DefaultControlTemplate;

        static FloorFilter()
        {
            var backgroundColor = Color.FromHex("#aaffffff");
            var foregroundColor = Color.FromHex("#6e6e6e");
            DefaultLevelDataTemplate = new DataTemplate(() =>
            {
                var viewcell = new ViewCell();

                Grid containingGrid = new Grid
                {
                    BackgroundColor = backgroundColor,
                    WidthRequest = 48,
                };
                containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });

                Label textLabel = new Label
                {
                    FontSize = 14,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = foregroundColor,
                };
                textLabel.SetBinding(Label.TextProperty, "ShortName");

                containingGrid.Children.Add(textLabel);
                viewcell.View = containingGrid;
                return viewcell;
            });

            DefaultFacilityDataTemplate = new DataTemplate(() =>
            {
                var viewcell = new ViewCell();
                Grid containingGrid = new Grid { BackgroundColor = backgroundColor };

                Label textLabel = new Label
                {
                    FontSize = 14,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = foregroundColor,
                    Margin = new Thickness(4),
                };
                textLabel.SetBinding(Label.TextProperty, "Name");

                containingGrid.Children.Add(textLabel);
                viewcell.View = containingGrid;
                return viewcell;
            });

            DefaultSiteDataTemplate = DefaultFacilityDataTemplate;

            DefaultDifferentiatingFacilityDataTemplate = new DataTemplate(() =>
            {
                var viewCell = new ViewCell();

                Grid containingGrid = new Grid
                {
                    Padding = new Thickness(8, 4, 4, 4),
                    BackgroundColor = backgroundColor,
                };

                containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                containingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid textStack = new Grid
                {
                    VerticalOptions = LayoutOptions.Center,
                    RowSpacing = 2,
                };

                textStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                textStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Label titleLabel = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.End,
                    VerticalTextAlignment = TextAlignment.End,
                    TextColor = foregroundColor,
                    FontSize = 14,
                };
                titleLabel.SetBinding(Label.TextProperty, "Name");

                Label subtitleLabel = new Label
                {
                    TextColor = Color.FromHex("#2e2e2e"),
                    FontSize = 11,
                    VerticalTextAlignment = TextAlignment.Start,
                    VerticalOptions = LayoutOptions.Start,
                };
                subtitleLabel.SetBinding(Label.TextProperty, "Site.Name");

                textStack.Children.Add(titleLabel);
                textStack.Children.Add(subtitleLabel);
                Grid.SetRow(titleLabel, 0);
                Grid.SetRow(subtitleLabel, 1);

                containingGrid.Children.Add(textStack);

                Grid.SetColumn(textStack, 1);

                viewCell.View = containingGrid;
                return viewCell;
            });

            string template =
$@"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" 
    xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms""
    x:DataType=""controls:FloorFilter"" x:Name=""Self"">
    <Grid RowSpacing=""0"" x:Name=""PART_VisibilityWrapper"" IsVisible=""True"" BackgroundColor=""White"" >
        <Grid.ColumnDefinitions >
            <ColumnDefinition Width=""48"" />
        </Grid.ColumnDefinitions >
        <Grid.RowDefinitions >
           <RowDefinition Height=""Auto"" />
           <RowDefinition Height=""Auto"" />
           <RowDefinition Height=""*"" />
           <RowDefinition Height=""Auto"" />
           <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions >
<Frame HasShadow=""False"" BackgroundColor=""White"" Grid.RowSpan=""5"" HorizontalOptions=""FillAndExpand"" VerticalOptions=""FillAndExpand"" />
        <Button x:Name=""{nameof(PART_BrowseButton)}"" Margin=""0"" WidthRequest=""48"" HeightRequest=""48""
           CornerRadius=""0"" BackgroundColor=""White"" Padding=""16"" FontFamily=""calcite-ui-icons-24""
           BorderColor=""#aa6e6e6e"" BorderWidth=""1"" Text=""{IconFont.UrbanModel}"" TextColor=""Accent"" />
        <Button x:Name=""{nameof(PART_AllButton)}"" Text=""{IconFont.Viewshed}"" Margin=""0,-1,0,0"" WidthRequest=""48"" HeightRequest=""48""
           BorderColor=""#aa6e6e6e"" BorderWidth=""1"" FontFamily=""calcite-ui-icons-24""
           CornerRadius=""0"" Background=""White"" TextColor=""Accent""
           Grid.Row=""1"" />
        <Frame x:Name=""{nameof(PART_LevelListContainer)}"" Grid.Row=""2"" BackgroundColor=""White"" BorderColor=""#aa6e6e6e""
           Padding=""1"" Margin=""0,-1,0,0"" IsClippedToBounds=""True""
           HasShadow=""False"" CornerRadius=""0"" >
              <ListView x:Name=""{nameof(PART_LevelListView)}""
                 Grid.Row=""2""
                 RowHeight=""48"" SeparatorColor=""#aa6e6e6e""
                 SeparatorVisibility=""None""
                 WidthRequest=""48""
                 Background=""White""
                 HeightRequest=""0""
                 Margin=""0"" />
        </Frame >
        <Button x:Name=""{nameof(PART_ZoomButton)}"" Text=""{IconFont.ZoomToObject}"" Margin=""0,-1,0,0"" WidthRequest=""48"" HeightRequest=""48""
           BorderColor=""#aa6e6e6e"" BorderWidth=""1"" FontFamily=""calcite-ui-icons-24""
           CornerRadius=""0"" Background=""White"" TextColor=""Accent""
           Grid.Row=""3""
           IsVisible=""True"" />
   </Grid >
</ControlTemplate >";
            DefaultControlTemplate = XForms.Extensions.LoadFromXaml(new ControlTemplate(), template);
        }
    }
}
