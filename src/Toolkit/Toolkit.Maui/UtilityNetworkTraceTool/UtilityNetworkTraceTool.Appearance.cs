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

using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class UtilityNetworkTraceTool
{
#pragma warning disable SA1310, SX1309, SA1306
    // Navigation
    private SegmentedControl? PART_NavigationSegment;

    // Select section
    private Layout? PART_SelectContainer;
    private Label? PART_LabelNetworks;
    private Picker? PART_ListViewNetworks;
    private Label? PART_LabelTraceTypes;
    private Picker? PART_ListViewTraceTypes;

    // Configure section
    private Layout? PART_ConfigureContainer;
    private Button? PART_ButtonAddStartingPoint;
    private Button? PART_ButtonCancelAddStartingPoint;
    private CollectionView? PART_ListViewStartingPoints;

    // Run section
    private Layout? PART_RunContainer;
    private Button? PART_ButtonRunTrace;

    // View section
    private Layout? PART_ViewContainer;
    private Grid? PART_GridResultsDisplay;

    // Warnings (multiple sections)
    private View? PART_DuplicateTraceWarning;
    private View? PART_ExtraStartingPointsWarning;
    private View? PART_NeedMoreStartingPointsWarning;
    private View? PART_NoResultsWarning;
    private View? PART_NoNetworksWarning;

    // Loading indicators and cancel
    private Frame? PART_ActivityIndicator;
    private Button? PART_ButtonCancelActivity;
#pragma warning restore SA1310, SX1309, SA1306

    private static readonly ControlTemplate DefaultControlTemplate;

    static UtilityNetworkTraceTool()
    {
        const string backgroundColor = "{AppThemeBinding Dark=#353535, Light=#F8F8F8}";
        const string foregroundColor = "{AppThemeBinding Dark=#ffffff, Light=#151515}";
        string template =
$@"<ControlTemplate xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
xmlns:ios=""clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;assembly=Microsoft.Maui.Controls""
xmlns:esriTKPrim=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui.Primitives;assembly=Esri.ArcGISRuntime.Toolkit.Maui""
xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui;assembly=Esri.ArcGISRuntime.Toolkit.Maui"">
<Grid RowSpacing=""8"" Padding=""8,0,8,0"">
    <Grid.Resources>
        <Style TargetType=""Frame"">
            <Setter Property=""Background"" Value=""{{AppThemeBinding Dark=#2b2b2b,Light=#fff}}"" />
        </Style>
    </Grid.Resources>
    <Grid.RowDefinitions>
        <RowDefinition Height=""0"" />
        <RowDefinition Height=""Auto"" />
        <RowDefinition Height=""*"" />
    </Grid.RowDefinitions>
    <Frame x:Name=""{nameof(PART_NoNetworksWarning)}"" BorderColor=""{{AppThemeBinding Light=#D83020, Dark=#FE583E}}"" CornerRadius=""4"" Margin=""4"" Grid.RowSpan=""3"" IsVisible=""false"">
        <Label Text=""No utility networks found."" />
    </Frame>
    <esriTKPrim:SegmentedControl x:Name=""{nameof(PART_NavigationSegment)}"" Grid.Row=""1"" HeightRequest=""30"" />
    <VerticalStackLayout x:Name=""{nameof(PART_SelectContainer)}"" Grid.Row=""2"" Spacing=""8"">
        <Label x:Name=""{nameof(PART_LabelNetworks)}"" Text=""Networks"" FontAttributes=""Bold"" IsVisible=""false"" />
        <Picker x:Name=""{nameof(PART_ListViewNetworks)}"" ItemDisplayBinding=""{{Binding Name}}"" IsVisible=""false"" BackgroundColor=""{{AppThemeBinding Light=#eaeaea,Dark=#151515}}"" Title=""Select a Utility Network"" />
        <Label x:Name=""{nameof(PART_LabelTraceTypes)}"" Text=""Named trace configurations"" FontAttributes=""Bold"" IsVisible=""false"" />
        <Picker x:Name=""{nameof(PART_ListViewTraceTypes)}"" IsVisible=""false"" ItemDisplayBinding=""{{Binding Name}}"" BackgroundColor=""{{AppThemeBinding Light=#eaeaea,Dark=#151515}}"" Title=""Select a trace configuration""  />
    </VerticalStackLayout>
    <Grid x:Name=""{nameof(PART_ConfigureContainer)}"" Grid.Row=""2"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
        </Grid.RowDefinitions>
        <Button x:Name=""{nameof(PART_ButtonAddStartingPoint)}"" Text=""Add starting point"" IsVisible=""false"" Grid.Row=""0"" />
        <Button x:Name=""{nameof(PART_ButtonCancelAddStartingPoint)}"" Text=""Cancel"" IsVisible=""false"" Grid.Row=""0""/>
        <CollectionView x:Name=""{nameof(PART_ListViewStartingPoints)}"" Background=""{backgroundColor}"" SelectionMode=""Single"" IsVisible=""false"" Grid.Row=""1"">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <Grid Padding=""4"">
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""Auto"" />
                        </Grid.RowDefinitions>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""40"" />
                            <ColumnDefinition Width=""*"" />
                            <ColumnDefinition Width=""40"" />
                        </Grid.ColumnDefinitions>
                        <esriTK:SymbolDisplay Symbol=""{{Binding Symbol}}"" Grid.RowSpan=""2"" />
                        <Label Grid.Column=""1"" Text=""{{Binding StartingPoint.NetworkSource.Name}}"" FontAttributes=""Bold"" TextColor=""{foregroundColor}""  />
                        <Label Grid.Column=""1"" Grid.Row=""1"" Text=""{{Binding StartingPoint.AssetGroup.Name}}"" TextColor=""{foregroundColor}""  />
                        <Button Grid.Column=""2"" Grid.RowSpan=""2"" Text=""{IconFont.XCircle}"" FontFamily=""calcite-ui-icons-24"" Command=""{{Binding DeleteCommand}}"" Padding=""2"" />
                        <Slider Grid.Row=""3"" Grid.ColumnSpan=""3"" Minimum=""0"" Maximum=""1.0"" Value=""{{Binding FractionAlongEdge, Mode=TwoWay}}"" IsVisible=""{{Binding FractionSliderVisible, Mode=OneWay}}"" Margin=""8"" />
                        <Picker Grid.Row=""3"" Grid.ColumnSpan=""3"" ItemsSource=""{{Binding StartingPoint.AssetType.TerminalConfiguration.Terminals}}"" ItemDisplayBinding=""{{Binding Name}}"" SelectedItem=""{{Binding StartingPoint.Terminal, Mode=TwoWay}}"" IsVisible=""{{Binding TerminalPickerVisible, Mode=OneWay}}"" Margin=""8"" />
                    </Grid>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

    </Grid>
    <Grid x:Name=""{nameof(PART_RunContainer)}"" Grid.Row=""2"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>
        <Frame x:Name=""{nameof(PART_NeedMoreStartingPointsWarning)}"" BorderColor=""{{AppThemeBinding Light=#EDD317,Dark=#FFC900}}"" CornerRadius=""4"" Margin=""4"" IsVisible=""false"" Grid.Row=""0"">
            <Label Text=""Not enough starting points. Use the 'Configure' section to add starting points."" TextColor=""{foregroundColor}""  />
        </Frame>
        <Frame x:Name=""{nameof(PART_ExtraStartingPointsWarning)}"" BorderColor=""{{AppThemeBinding Light=#007AC2, Dark=#009AF2}}"" CornerRadius=""4"" Margin=""4"" IsVisible=""false"" Grid.Row=""0"">
            <Label Text=""There are more starting points than required for the selected trace configuration."" TextColor=""{foregroundColor}""  />
        </Frame>
        <Frame x:Name=""{nameof(PART_DuplicateTraceWarning)}"" BorderColor=""{{AppThemeBinding Light=#EDD317,Dark=#FFC900}}"" CornerRadius=""4"" Margin=""4"" IsVisible=""false"" Grid.Row=""0"">
            <Label Text=""The selected trace configuration has already been run with the selected starting points."" TextColor=""{foregroundColor}""  />
        </Frame>
        <Button x:Name=""{nameof(PART_ButtonRunTrace)}"" Text=""Run Trace"" IsVisible=""false"" Grid.Row=""2"" />
    </Grid>
    <Grid x:Name=""{nameof(PART_ViewContainer)}"" Grid.Row=""2"">
        <Frame x:Name=""{nameof(PART_NoResultsWarning)}"" BorderColor=""{{AppThemeBinding Light=#D83020, Dark=#FE583E}}"" CornerRadius=""4"" Margin=""4"" IsVisible=""false"">
            <Label Text=""No results."" TextColor=""{foregroundColor}""  />
        </Frame>
        <Grid x:Name=""{nameof(PART_GridResultsDisplay)}"" IsVisible=""false"">
            <BindableLayout.ItemTemplate>
                <DataTemplate>
                    <ScrollView>
                    <StackLayout Spacing=""8"">
                        <Label Text=""{{Binding Name}}"" FontSize=""18"" FontAttributes=""Bold"" TextColor=""{foregroundColor}""  />
                        <Label Text=""Function results"" FontSize=""14"" TextColor=""{foregroundColor}""  />
                        <StackLayout BindableLayout.ItemsSource=""{{Binding FunctionResults}}"">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate>
                                    <Grid Padding=""8"">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""*"" />
                                            <ColumnDefinition Width=""Auto"" />
                                        </Grid.ColumnDefinitions>
                                        <Grid.RowDefinitions>
                                            <RowDefinition Height=""Auto"" />
                                            <RowDefinition Height=""Auto"" />
                                        </Grid.RowDefinitions>
                                        <Label Text=""{{Binding Function.NetworkAttribute.Name}}"" FontAttributes=""Bold"" TextColor=""{foregroundColor}""  />
                                        <Label Text=""{{Binding Result}}"" Grid.Column=""1"" TextColor=""{foregroundColor}""  />
                                        <Label Text=""{{Binding Function.FunctionType}}"" Grid.Row=""1"" Grid.ColumnSpan=""2"" TextColor=""{foregroundColor}""  />
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                            <BindableLayout.EmptyViewTemplate>
                                <DataTemplate>
                                    <Frame BorderColor=""#D83020"" CornerRadius=""4"" Margin=""4"" Grid.RowSpan=""3"">
                                        <Label Text=""No function results."" HorizontalOptions=""Center"" Padding=""16"" TextColor=""{foregroundColor}""  />
                                    </Frame> 
                                </DataTemplate>
                            </BindableLayout.EmptyViewTemplate>
                        </StackLayout>
                        <Label Text=""Feature results"" FontSize=""14"" />
                        <StackLayout BindableLayout.ItemsSource=""{{Binding ElementResultsGrouped}}"">
                            <BindableLayout.ItemTemplate>
                                <DataTemplate>
                                    <Grid Padding=""8"">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""*"" />
                                            <ColumnDefinition Width=""Auto"" />
                                        </Grid.ColumnDefinitions>
                                        <Label Text=""{{Binding Item1.Name}}"" FontAttributes=""Bold"" TextColor=""{foregroundColor}""  />
                                        <Label Text=""{{Binding Item2}}"" Grid.Column=""1"" TextColor=""{foregroundColor}""  />
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                            <BindableLayout.EmptyViewTemplate>
                                <DataTemplate>
                                    <Label Text=""No feature results"" HorizontalOptions=""Center"" Padding=""16"" TextColor=""{foregroundColor}""  />
                                </DataTemplate>
                            </BindableLayout.EmptyViewTemplate>
                        </StackLayout>
                        <Label Text=""Visualization options"" FontSize=""14"" />
                        <StackLayout Orientation=""Horizontal"" Spacing=""4"">
                            <CheckBox IsChecked=""{{Binding AreGraphicsShown}}"" />
                            <Label Text=""Show graphics on map"" VerticalOptions=""Center"" TextColor=""{foregroundColor}""  />
                        </StackLayout>
                        <StackLayout Orientation=""Horizontal"" Spacing=""4"">
                            <CheckBox IsChecked=""{{Binding AreFeaturesSelected}}"" />
                            <Label Text=""Select features on map"" VerticalOptions=""Center""  TextColor=""{foregroundColor}"" />
                        </StackLayout>
                        <Button Text=""Delete result"" Command=""{{Binding DeleteCommand}}"" CommandParameter=""{{Binding}}"" />
                    </StackLayout>
                    </ScrollView>
                </DataTemplate>
            </BindableLayout.ItemTemplate>
        </Grid>
    </Grid>
    <Frame x:Name=""{nameof(PART_ActivityIndicator)}"" IsVisible=""false"" Grid.RowSpan=""3"" VerticalOptions=""FillAndExpand"" HorizontalOptions=""FillAndExpand"" Background=""{backgroundColor}"" HasShadow=""False"" CornerRadius=""0"" BorderColor=""{backgroundColor}"">
        <StackLayout Spacing=""8"" VerticalOptions=""Center"" HorizontalOptions=""CenterAndExpand"">
            <ActivityIndicator IsRunning=""True"" Color=""{{AppThemeBinding Light=#007AC2,Dark=#009AF2}}"" />
            <Button x:Name=""{nameof(PART_ButtonCancelActivity)}"" Text=""Cancel"" />
        </StackLayout>
    </Frame>
</Grid>
</ControlTemplate>";
        DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);
    }
}
