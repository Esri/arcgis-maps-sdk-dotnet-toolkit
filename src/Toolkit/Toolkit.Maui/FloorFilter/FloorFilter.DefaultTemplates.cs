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

using Esri.ArcGISRuntime.Mapping.Floor;
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class FloorFilter
{
    private static readonly DataTemplate DefaultLevelDataTemplate;
    private static readonly DataTemplate DefaultFacilityDataTemplate;
    private static readonly DataTemplate DefaultSiteDataTemplate;
    private static readonly DataTemplate DefaultDifferentiatingFacilityDataTemplate;
    private static readonly ControlTemplate DefaultControlTemplate;


    [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.Floor.FloorLevel.ShortName), "Esri.ArcGISRuntime.Mapping.Floor.FloorLevel", "Esri.ArcGISRuntime")]
    [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.Floor.FloorFacility.Name), "Esri.ArcGISRuntime.Mapping.Floor.FloorFacility", "Esri.ArcGISRuntime")]
    [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.Floor.FloorFacility.Site), "Esri.ArcGISRuntime.Mapping.Floor.FloorFacility", "Esri.ArcGISRuntime")]
    [DynamicDependency(nameof(Esri.ArcGISRuntime.Mapping.Floor.FloorSite.Name), "Esri.ArcGISRuntime.Mapping.Floor.FloorSite", "Esri.ArcGISRuntime")]
    static FloorFilter()
    {
        DefaultLevelDataTemplate = new DataTemplate(() =>
        {
            Grid containingGrid = new Grid
            {
                WidthRequest = 48,
                HeightRequest = 48,
                InputTransparent = false,
                CascadeInputTransparent = false,
#if WINDOWS
                Margin = new Thickness(-21, 0, 21, 0),
#endif
            };
            containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            Label textLabel = new Label
            {
                FontSize = 14,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                InputTransparent = false,
            };
            textLabel.SetBinding(Label.TextProperty, static (FloorLevel level) => level.ShortName);
            containingGrid.Children.Add(textLabel);
            return containingGrid;
        });

        DefaultFacilityDataTemplate = new DataTemplate(() =>
        {
            Grid containingGrid = new Grid();
            containingGrid.SetAppThemeColor(Grid.BackgroundColorProperty, Color.FromArgb("#FFF"), Color.FromArgb("#353535"));

            Label textLabel = new Label
            {
                FontSize = 14,
                VerticalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(8),
                HorizontalOptions = LayoutOptions.Fill
            };
            textLabel.SetBinding(Label.TextProperty, static (FloorFacility facility) => facility.Name);
            textLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#6e6e6e"), Color.FromArgb("#fff"));

            containingGrid.Children.Add(textLabel);
            return containingGrid;
        });

        DefaultSiteDataTemplate = DefaultFacilityDataTemplate;

        DefaultDifferentiatingFacilityDataTemplate = new DataTemplate(() =>
        {
            Grid containingGrid = new Grid
            {
                Padding = new Thickness(8),
            };
            containingGrid.SetAppThemeColor(Grid.BackgroundColorProperty, Color.FromArgb("#FFF"), Color.FromArgb("#353535"));

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
                FontSize = 14,
            };
            titleLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#6e6e6e"), Color.FromArgb("#fff"));
            titleLabel.SetBinding(Label.TextProperty, static (FloorFacility facility) => facility.Name);

            Label subtitleLabel = new Label
            {
                FontSize = 11,
                VerticalTextAlignment = TextAlignment.Start,
                VerticalOptions = LayoutOptions.Start,
            };
            subtitleLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#2e2e2e"), Color.FromArgb("#aaa"));
            subtitleLabel.SetBinding(Label.TextProperty, static (FloorFacility facility) => facility.Site?.Name);
            textStack.Children.Add(titleLabel);
            textStack.Children.Add(subtitleLabel);
            Grid.SetRow(titleLabel, 0);
            Grid.SetRow(subtitleLabel, 1);

            containingGrid.Children.Add(textStack);

            Grid.SetColumn(textStack, 1);
            return containingGrid;
        });

        string template =
$@"<ControlTemplate xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" 
    xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui""
    x:DataType=""controls:FloorFilter"" x:Name=""Self"">
    <Grid RowSpacing=""0"" x:Name=""PART_VisibilityWrapper"" IsVisible=""True"" VerticalOptions=""EndAndExpand"">
        <Grid.Resources>
            <Style TargetType=""Button"">
                <Setter Property=""Background"" Value=""{{AppThemeBinding Light=#fff,Dark=#353535}}"" />
                <Setter Property=""TextColor"" Value=""{{AppThemeBinding Light=#6e6e6e,Dark=#fff}}"" />
                <Setter Property=""CornerRadius"" Value=""0"" />
                <Setter Property=""BorderWidth"" Value=""1"" />
                <Setter Property=""BorderColor"" Value=""{{AppThemeBinding Light=#dfdfdf, Dark=#404040}}"" />
                <Setter Property=""FontFamily"" Value=""calcite-ui-icons-24"" />
                <Setter Property=""HeightRequest"" Value=""48"" />
                <Setter Property=""WidthRequest"" Value=""48"" />
            </Style>
        </Grid.Resources>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""48"" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
           <RowDefinition Height=""Auto"" />
           <RowDefinition Height=""Auto"" />
           <RowDefinition Height=""*"" />
           <RowDefinition Height=""Auto"" />
           <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>
<Border BackgroundColor=""{{AppThemeBinding Light=#dfdfdf, Dark=#404040}}"" Grid.RowSpan=""5"" HorizontalOptions=""FillAndExpand"" VerticalOptions=""FillAndExpand"" />
        <Button x:Name=""{nameof(PART_BrowseButton)}"" Margin=""0"" Padding=""16"" Text=""{IconFont.UrbanModel}"" />
        <Button x:Name=""{nameof(PART_AllButton)}"" Text=""{IconFont.Viewshed}"" Margin=""0,-1,0,0""
           Grid.Row=""1"" />
        <Border x:Name=""{nameof(PART_LevelListContainer)}"" Grid.Row=""2"" Stroke=""{{AppThemeBinding Light=#dfdfdf, Dark=#404040}}"" StrokeThickness=""1"" StrokeShape=""Rectangle""
           Padding=""-1,0,0,0"" Margin=""0,-1,0,0"">
                <Border.Resources>
                    <Style TargetType=""Grid"">
                        <Setter Property=""VisualStateManager.VisualStateGroups"">
                            <VisualStateGroupList>
                                <VisualStateGroup x:Name=""CommonStates"">
                                    <VisualState x:Name=""Normal"">
                                        <VisualState.Setters>
                                            <Setter Property=""BackgroundColor""
                                                    Value=""{{AppThemeBinding Light=#fff,Dark=#353535}}"" />
                                        </VisualState.Setters>
                                    </VisualState>
                                    <VisualState x:Name=""Selected"">
                                        <VisualState.Setters>
                                            <Setter Property=""BackgroundColor""
                                                    Value=""{{AppThemeBinding Light=#e2f1fb,Dark=#009af2}}"" />
                                        </VisualState.Setters>
                                    </VisualState>
                                </VisualStateGroup>
                            </VisualStateGroupList>
                        </Setter>
                    </Style>
                    <Style TargetType=""Label"">
                        <Setter Property=""TextColor"" Value=""{{AppThemeBinding Light=#6e6e6e,Dark=#fff}}"" />
                    </Style>
                </Border.Resources>
              <CollectionView x:Name=""{nameof(PART_LevelListView)}""
                 Grid.Row=""2""
                 SelectionMode=""Single""
                 VerticalOptions=""End""
                 WidthRequest=""46""
                 Margin=""0"" />
        </Border>
        <Button x:Name=""{nameof(PART_ZoomButton)}"" Text=""{IconFont.ZoomToObject}"" Margin=""0,-1,0,0""
           Grid.Row=""3""
           IsVisible=""True"" />
   </Grid>
</ControlTemplate>";
        DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);
    }
}
