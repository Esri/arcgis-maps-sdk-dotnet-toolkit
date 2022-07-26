using Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Primitives;
using System;
using Xamarin.Forms;
using XForms = Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    public partial class UtilityNetworkTraceTool
    {
        private Label? PART_NetworksListLabel;
        private ListView? PART_NetworksCollectionView;
        private Label? PART_TraceTypesLabel;
        private ListView? PART_TraceTypesListView;
        private Frame? PART_ActivityIndicator;
        private Button? PART_AddStartingPointButton;
        private ListView? PART_StartingPointListViewUWP;
        private Button? PART_RunTraceButton;
        private Button? PART_CancelAddStartingPointButton;
        private Button? PART_CancelWaitButton;
        private SegmentedControl? PART_NavigationSegment;
        private View? PART_DuplicateTraceWarningContainer;
        private View? PART_ExtraStartingPointsWarningContainer;
        private View? PART_NeedMoreStartingPointsWarningContainer;
        private View? PART_NoResultsWarning;
        private View? PART_NoNetworksWarning;
        // UWP helper
        private Grid? PART_ResultDisplayUWP;

        private static readonly ControlTemplate DefaultControlTemplate;

        static UtilityNetworkTraceTool()
        {
            string template =
$@"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
xmlns:ios=""clr-namespace:Xamarin.Forms.PlatformConfiguration.iOSSpecific;assembly=Xamarin.Forms.Core""
xmlns:esriTKPrim=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Primitives;assembly=Esri.ArcGISRuntime.Toolkit.Xamarin.Forms""
xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms;assembly=Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
<Grid RowSpacing=""8"" Padding=""8,0,8,0"">
    <Grid.RowDefinitions>
        <RowDefinition Height=""0"" />
        <RowDefinition Height=""Auto"" />
        <RowDefinition Height=""*"" />
    </Grid.RowDefinitions>
    <Frame x:Name=""{nameof(PART_NoNetworksWarning)}"" BorderColor=""#D83020"" CornerRadius=""4"" Margin=""4"" Grid.RowSpan=""3"">
        <Label Text=""No utility networks found."" />
    </Frame>
    <esriTKPrim:SegmentedControl x:Name=""{nameof(PART_NavigationSegment)}"" Grid.Row=""1"" HeightRequest=""30"" Padding=""0"" />
    <StackLayout Spacing=""8"" Grid.Row=""2"">
        <Label x:Name=""{nameof(PART_NetworksListLabel)}"" Text=""Networks"" FontAttributes=""Bold"" />
        <ListView x:Name=""{nameof(PART_NetworksCollectionView)}"" ios:ListView.SeparatorStyle=""FullWidth"" Background=""#dfdfdf"">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <ViewCell>
                        <Label Text=""{{Binding Name}}"" Padding=""8,4,8,4"" />
                    </ViewCell>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        <Label x:Name=""{nameof(PART_TraceTypesLabel)}"" Text=""Named trace configurations"" FontAttributes=""Bold"" />
        <ListView x:Name=""{nameof(PART_TraceTypesListView)}"" ios:ListView.SeparatorStyle=""FullWidth"" Background=""#dfdfdf""> 
            <ListView.ItemTemplate>
                <DataTemplate>
                    <ViewCell>
                        <Label Text=""{{Binding Name}}"" Padding=""8"" />
                    </ViewCell>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        <Button x:Name=""{nameof(PART_AddStartingPointButton)}"" Text=""Add starting point"" />
        <Button x:Name=""{nameof(PART_CancelAddStartingPointButton)}"" Text=""Cancel"" />
        <ListView x:Name=""{nameof(PART_StartingPointListViewUWP)}"" ios:ListView.SeparatorStyle=""FullWidth"" RowHeight=""64"" HasUnevenRows=""True"" Background=""#dfdfdf"">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <ViewCell>
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
                            <Label Grid.Column=""1"" Text=""{{Binding StartingPoint.NetworkSource.Name}}"" FontAttributes=""Bold"" />
                            <Label Grid.Column=""1"" Grid.Row=""1"" Text=""{{Binding StartingPoint.AssetGroup.Name}}"" />
                            <Button Grid.Column=""2"" Grid.RowSpan=""2"" Text=""X"" Command=""{{Binding DeleteCommand}}"" Padding=""2"" />
                        </Grid>
                    </ViewCell>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        <Frame x:Name=""{nameof(PART_NeedMoreStartingPointsWarningContainer)}"" BorderColor=""#EDD317"" CornerRadius=""4"" Margin=""4"">
            <Label Text=""Not enough starting points. Use the 'Configure' section to add starting points."" />
        </Frame>
        <Frame x:Name=""{nameof(PART_ExtraStartingPointsWarningContainer)}"" BorderColor=""#007AC2"" CornerRadius=""4"" Margin=""4"">
            <Label Text=""There are more starting points than required for the selected trace configuration."" />
        </Frame>
        <Frame x:Name=""{nameof(PART_DuplicateTraceWarningContainer)}"" BorderColor=""#EDD317"" CornerRadius=""4"" Margin=""4"">
            <Label Text=""The selected trace configuration has already been run with the selected starting points."" />
        </Frame>
        <Button x:Name=""{nameof(PART_RunTraceButton)}"" Text=""Run Trace"" />
        <Frame x:Name=""{nameof(PART_NoResultsWarning)}"" BorderColor=""#D83020"" CornerRadius=""4"" Margin=""4"">
            <Label Text=""No results."" />
        </Frame>
        <Grid x:Name=""{nameof(PART_ResultDisplayUWP)}"" IsVisible=""false"">
                        <BindableLayout.ItemTemplate>
                <DataTemplate>
                    <ScrollView>
                    <StackLayout Spacing=""8"">
                        <Label Text=""{{Binding Name}}"" FontSize=""18"" FontAttributes=""Bold"" />
                        <Label Text=""Function results"" FontSize=""14"" />
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
                                        <Label Text=""{{Binding Function.NetworkAttribute.Name}}"" FontAttributes=""Bold"" />
                                        <Label Text=""{{Binding Result}}"" Grid.Column=""1"" />
                                        <Label Text=""{{Binding Function.FunctionType}}"" Grid.Row=""1"" Grid.ColumnSpan=""2"" />
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                            <BindableLayout.EmptyViewTemplate>
                                <DataTemplate>
                                    <Frame BorderColor=""#D83020"" CornerRadius=""4"" Margin=""4"" Grid.RowSpan=""3"">
                                        <Label Text=""No function results."" HorizontalOptions=""Center"" Padding=""16"" />
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
                                        <Label Text=""{{Binding Key.Name}}"" FontAttributes=""Bold"" />
                                        <Label Text=""{{Binding Count}}"" Grid.Column=""1"" />
                                    </Grid>
                                </DataTemplate>
                            </BindableLayout.ItemTemplate>
                            <BindableLayout.EmptyViewTemplate>
                                <DataTemplate>
                                    <Label Text=""No feature results"" HorizontalOptions=""Center"" Padding=""16"" />
                                </DataTemplate>
                            </BindableLayout.EmptyViewTemplate>
                        </StackLayout>
                        <Label Text=""Visualization options"" FontSize=""14"" />
                        <StackLayout Orientation=""Horizontal"" Spacing=""8"">
                            <CheckBox IsChecked=""{{Binding AreGraphicsShown}}"" />
                            <Label Text=""Show graphics on map"" VerticalOptions=""Center"" />
                        </StackLayout>
                        <StackLayout Orientation=""Horizontal"" Spacing=""8"">
                            <CheckBox IsChecked=""{{Binding AreFeaturesSelected}}"" />
                            <Label Text=""Select features on map"" VerticalOptions=""Center"" />
                        </StackLayout>
                        <Button Text=""Delete result"" Command=""{{Binding DeleteCommand}}"" CommandParameter=""{{Binding}}"" />
                    </StackLayout>
                    </ScrollView>
                </DataTemplate>
            </BindableLayout.ItemTemplate>
        </Grid>
    </StackLayout>
    <Frame x:Name=""{nameof(PART_ActivityIndicator)}"" IsVisible=""false"" Grid.RowSpan=""3"" VerticalOptions=""FillAndExpand"" HorizontalOptions=""FillAndExpand"" Background=""#eaeaea"" HasShadow=""False"" CornerRadius=""0"" BorderColor=""#eaeaea"">
        <StackLayout Spacing=""8"" VerticalOptions=""Center"" HorizontalOptions=""CenterAndExpand"">
            <ActivityIndicator IsRunning=""True"" />
            <Button x:Name=""{nameof(PART_CancelWaitButton)}"" Text=""Cancel"" />
        </StackLayout>
    </Frame>
</Grid>
</ControlTemplate>";
            DefaultControlTemplate = XForms.Extensions.LoadFromXaml(new ControlTemplate(), template);
        }
    }
}

