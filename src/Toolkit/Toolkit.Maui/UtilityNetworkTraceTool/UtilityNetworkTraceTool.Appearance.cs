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
using System.Diagnostics.CodeAnalysis;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

#pragma warning disable CA1001
public partial class UtilityNetworkTraceTool
#pragma warning restore CA1001
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
    private Border? PART_ActivityIndicator;
    private Button? PART_ButtonCancelActivity;
#pragma warning restore SA1310, SX1309, SA1306

    private static readonly ControlTemplate DefaultControlTemplate;

    [DynamicDependency(nameof(Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunctionOutput.Function), "Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunctionOutput", "Esri.ArcGISRuntime")]
    [DynamicDependency(nameof(Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunctionOutput.Result), "Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunctionOutput", "Esri.ArcGISRuntime")]
    [DynamicDependency(nameof(Esri.ArcGISRuntime.UtilityNetworks.UtilityNetworkAttribute.Name), "Esri.ArcGISRuntime.UtilityNetworks.UtilityNetworkAttribute", "Esri.ArcGISRuntime")]
    [DynamicDependency(nameof(Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunction.FunctionType), "Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunction", "Esri.ArcGISRuntime")]
    [DynamicDependency(nameof(Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunction.NetworkAttribute), "Esri.ArcGISRuntime.UtilityNetworks.UtilityTraceFunction", "Esri.ArcGISRuntime")]
    static UtilityNetworkTraceTool()
    {
        const string backgroundColor = "{AppThemeBinding Dark=#353535, Light=#F8F8F8}";
        const string foregroundColor = "{AppThemeBinding Dark=#ffffff, Light=#151515}";

        var noUtilityNetworks = Properties.Resources.GetString("UtilityNetworkTraceToolNoUtilityNetworks");
        var utilityNetworks = Properties.Resources.GetString("UtilityNetworkTraceToolUtilityNetworks");
        var traceTypes = Properties.Resources.GetString("UtilityNetworkTraceToolTraceTypes");
        var addStartingPoint = Properties.Resources.GetString("UtilityNetworkTraceToolAddStartingPoint");
        var cancel = Properties.Resources.GetString("UtilityNetworkTraceToolCancel");
        var notEnoughStartingPoints = Properties.Resources.GetString("UtilityNetworkTraceToolNotEnoughStartingPoints");
        var moreThanRequiredStartingPoints = Properties.Resources.GetString("UtilityNetworkTraceToolMoreThanRequiredStartingPoints");
        var duplicateTrace = Properties.Resources.GetString("UtilityNetworkTraceToolDuplicateTrace");
        var runTrace = Properties.Resources.GetString("UtilityNetworkTraceToolRunTrace");
        var noResults = Properties.Resources.GetString("UtilityNetworkTraceToolNoResults");
        var noFeatureResults = Properties.Resources.GetString("UtilityNetworkTraceToolNoFeatureResults");
        var noFunctionResults = Properties.Resources.GetString("UtilityNetworkTraceToolNoFunctionResults");
        var featureResults = Properties.Resources.GetString("UtilityNetworkTraceToolFeatureResults");
        var functionResults = Properties.Resources.GetString("UtilityNetworkTraceToolFunctionResults");
        var visualizationOptions = Properties.Resources.GetString("UtilityNetworkTraceToolVisualizationOptions");
        var showGraphics = Properties.Resources.GetString("UtilityNetworkTraceToolShowGraphics");
        var selectFeatures = Properties.Resources.GetString("UtilityNetworkTraceToolSelectFeatures");
        var discardResult = Properties.Resources.GetString("UtilityNetworkTraceToolDiscardResult");

        string template =
$@"<ControlTemplate xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
xmlns:ios=""clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;assembly=Microsoft.Maui.Controls""
xmlns:esriTKPrim=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui.Primitives;assembly=Esri.ArcGISRuntime.Toolkit.Maui""
xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui;assembly=Esri.ArcGISRuntime.Toolkit.Maui"">
<Grid RowSpacing=""8"" Padding=""8,0,8,0"">
    <Grid.Resources>
        <Style TargetType=""Border"">
            <Setter Property=""Background"" Value=""{{AppThemeBinding Dark=#2b2b2b,Light=#fff}}"" />
        </Style>
    </Grid.Resources>
    <Grid.RowDefinitions>
        <RowDefinition Height=""0"" />
        <RowDefinition Height=""Auto"" />
        <RowDefinition Height=""*"" />
    </Grid.RowDefinitions>
    <Border x:Name=""{nameof(PART_NoNetworksWarning)}"" Stroke=""{{AppThemeBinding Light=#D83020, Dark=#FE583E}}"" StrokeShape=""RoundRectangle 4"" Margin=""4"" Padding=""20"" Grid.RowSpan=""3"" IsVisible=""false"">
        <Label Text=""{noUtilityNetworks}"" />
    </Border>
    <esriTKPrim:SegmentedControl x:Name=""{nameof(PART_NavigationSegment)}"" Grid.Row=""1"" HeightRequest=""30"" />
    <VerticalStackLayout x:Name=""{nameof(PART_SelectContainer)}"" Grid.Row=""2"" Spacing=""8"">
        <Label x:Name=""{nameof(PART_LabelNetworks)}"" Text=""{utilityNetworks}"" FontAttributes=""Bold"" IsVisible=""false"" />
        <Picker x:Name=""{nameof(PART_ListViewNetworks)}"" ItemDisplayBinding=""{{Binding Name}}"" IsVisible=""false"" BackgroundColor=""{{AppThemeBinding Light=#eaeaea,Dark=#151515}}"" Title=""Select a Utility Network"" />
        <Label x:Name=""{nameof(PART_LabelTraceTypes)}"" Text=""{traceTypes}"" FontAttributes=""Bold"" IsVisible=""false"" />
        <Picker x:Name=""{nameof(PART_ListViewTraceTypes)}"" IsVisible=""false"" ItemDisplayBinding=""{{Binding Name}}"" BackgroundColor=""{{AppThemeBinding Light=#eaeaea,Dark=#151515}}"" Title=""Select a trace configuration""  />
    </VerticalStackLayout>
    <Grid x:Name=""{nameof(PART_ConfigureContainer)}"" Grid.Row=""2"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
        </Grid.RowDefinitions>
        <Button x:Name=""{nameof(PART_ButtonAddStartingPoint)}"" Text=""{addStartingPoint}"" IsVisible=""false"" Grid.Row=""0"" />
        <Button x:Name=""{nameof(PART_ButtonCancelAddStartingPoint)}"" Text=""{cancel}"" IsVisible=""false"" Grid.Row=""0""/>
        <CollectionView x:Name=""{nameof(PART_ListViewStartingPoints)}"" Background=""{backgroundColor}"" SelectionMode=""Single"" IsVisible=""false"" Grid.Row=""1"">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <Grid Padding=""4"">
                        <Grid.RowDefinitions>
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
                        <Button Grid.Column=""2"" Grid.RowSpan=""2"" Text=""X"" Command=""{{Binding DeleteCommand}}"" Padding=""2"" />
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
        <Border x:Name=""{nameof(PART_NeedMoreStartingPointsWarning)}"" Stroke=""{{AppThemeBinding Light=#EDD317,Dark=#FFC900}}"" StrokeShape=""RoundRectangle 4"" Margin=""4"" Padding=""20"" IsVisible=""false"" Grid.Row=""0"">
            <Label Text=""{notEnoughStartingPoints}"" TextColor=""{foregroundColor}""  />
        </Border>
        <Border x:Name=""{nameof(PART_ExtraStartingPointsWarning)}"" Stroke=""{{AppThemeBinding Light=#007AC2, Dark=#009AF2}}"" StrokeShape=""RoundRectangle 4"" Margin=""4"" Padding=""20"" IsVisible=""false"" Grid.Row=""0"">
            <Label Text=""{moreThanRequiredStartingPoints}"" TextColor=""{foregroundColor}""  />
        </Border>
        <Border x:Name=""{nameof(PART_DuplicateTraceWarning)}"" Stroke=""{{AppThemeBinding Light=#EDD317,Dark=#FFC900}}"" StrokeShape=""RoundRectangle 4"" Margin=""4"" Padding=""20"" IsVisible=""false"" Grid.Row=""0"">
            <Label Text=""{duplicateTrace}"" TextColor=""{foregroundColor}""  />
        </Border>
        <Button x:Name=""{nameof(PART_ButtonRunTrace)}"" Text=""{runTrace}"" IsVisible=""false"" Grid.Row=""2"" />
    </Grid>
    <Grid x:Name=""{nameof(PART_ViewContainer)}"" Grid.Row=""2"">
        <Border x:Name=""{nameof(PART_NoResultsWarning)}"" Stroke=""{{AppThemeBinding Light=#D83020, Dark=#FE583E}}"" StrokeShape=""RoundRectangle 4"" Margin=""4"" Padding=""20"" IsVisible=""false"">
            <Label Text=""{noResults}"" TextColor=""{foregroundColor}""  />
        </Border>
        <Grid x:Name=""{nameof(PART_GridResultsDisplay)}"" IsVisible=""false"">
            <BindableLayout.ItemTemplate>
                <DataTemplate>
                    <ScrollView>
                    <StackLayout Spacing=""8"">
                        <Label Text=""{{Binding Name}}"" FontSize=""18"" FontAttributes=""Bold"" TextColor=""{foregroundColor}""  />
                        <Label Text=""{functionResults}"" FontSize=""14"" TextColor=""{foregroundColor}""  />
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
                                    <Border Stroke=""#D83020"" StrokeShape=""RoundRectangle 4"" Margin=""4"" Grid.RowSpan=""3"">
                                        <Label Text=""{noFunctionResults}"" HorizontalOptions=""Center"" Padding=""16"" TextColor=""{foregroundColor}""  />
                                    </Border> 
                                </DataTemplate>
                            </BindableLayout.EmptyViewTemplate>
                        </StackLayout>
                        <Label Text=""{featureResults}"" FontSize=""14"" />
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
                                    <Label Text=""{noFeatureResults}"" HorizontalOptions=""Center"" Padding=""16"" TextColor=""{foregroundColor}""  />
                                </DataTemplate>
                            </BindableLayout.EmptyViewTemplate>
                        </StackLayout>
                        <Label Text=""{visualizationOptions}"" FontSize=""14"" />
                        <StackLayout Orientation=""Horizontal"" Spacing=""4"">
                            <CheckBox IsChecked=""{{Binding AreGraphicsShown}}"" />
                            <Label Text=""{showGraphics}"" VerticalOptions=""Center"" TextColor=""{foregroundColor}""  />
                        </StackLayout>
                        <StackLayout Orientation=""Horizontal"" Spacing=""4"">
                            <CheckBox IsChecked=""{{Binding AreFeaturesSelected}}"" />
                            <Label Text=""{selectFeatures}"" VerticalOptions=""Center""  TextColor=""{foregroundColor}"" />
                        </StackLayout>
                        <Button Text=""{discardResult}"" Command=""{{Binding DeleteCommand}}"" CommandParameter=""{{Binding}}"" />
                    </StackLayout>
                    </ScrollView>
                </DataTemplate>
            </BindableLayout.ItemTemplate>
        </Grid>
    </Grid>
    <Border x:Name=""{nameof(PART_ActivityIndicator)}"" IsVisible=""false"" Grid.RowSpan=""3"" VerticalOptions=""FillAndExpand"" HorizontalOptions=""FillAndExpand"" Background=""{backgroundColor}"" HasShadow=""False"" Stroke=""{backgroundColor}"">
        <StackLayout Spacing=""8"" VerticalOptions=""Center"" HorizontalOptions=""CenterAndExpand"">
            <ActivityIndicator IsRunning=""True"" Color=""{{AppThemeBinding Light=#007AC2,Dark=#009AF2}}"" />
            <Button x:Name=""{nameof(PART_ButtonCancelActivity)}"" Text=""{cancel}"" />
        </StackLayout>
    </Border>
</Grid>
</ControlTemplate>";
        DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);
    }
}
