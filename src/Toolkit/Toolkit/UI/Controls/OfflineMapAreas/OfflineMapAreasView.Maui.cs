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
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Esri.ArcGISRuntime.Toolkit.Maui.Primitives;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class OfflineMapAreasView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly DataTemplate DefaultItemTemplate;
        private static readonly ByteArrayToImageSourceConverter ImageSourceConverter = new();

        /// <summary>
        /// Template name of the <see cref="ItemsView"/> items layout view.
        /// </summary>
        public const string ItemsViewName = "MapAreasView";

        private const string RefreshMapAreasButtonName = "RefreshMapAreasButton";
        private const string NoInternetRefreshButtonName = "NoInternetRefreshButton";
        private const string AddMapAreaButtonName = "AddMapAreaButton";
        private const string AddAreaMapViewName = "AddAreaMapView";
        private const string AddOnDemandAreaNameTextBoxName = "AddOnDemandAreaNameTextBox";
        private const string AcceptAddOnDemandAreaButtonName = "AcceptAddOnDemandAreaButton";
        private const string CancelAddOnDemandAreaButtonName = "CancelAddOnDemandAreaButton";

        private Button? _refreshMapAreasButton;
        private Button? _noInternetRefreshButton;
        private Button? _addMapAreaButton;
        private Button? _acceptAddOnDemandAreaButton;
        private Button? _cancelAddOnDemandAreaButton;

        static OfflineMapAreasView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
            DefaultItemTemplate = new DataTemplate(BuildMapAreasItemTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            string offlineDisabledTitle = Properties.Resources.GetString("OfflineMapAreasOfflineDisabledTitle")!;
            string offlineDisabledMessage = Properties.Resources.GetString("OfflineMapAreasOfflineDisabledMessage")!;
            string noInternetConnectionTitle = Properties.Resources.GetString("OfflineMapAreasNoInternetConnectionTitle")!;
            string noInternetConnectionMessage = Properties.Resources.GetString("OfflineMapAreasNoInternetConnectionMessage")!;
            string refresh = Properties.Resources.GetString("OfflineMapAreasRefresh")!;
            string noMapAreasTitle = Properties.Resources.GetString("OfflineMapAreasNoMapAreasTitle")!;
            string noMapAreasPreplannedMessage = Properties.Resources.GetString("OfflineMapAreasNoMapAreasPreplannedMessage")!;
            string noMapAreasOnDemandMessage = Properties.Resources.GetString("OfflineMapAreasNoMapAreasOnDemandMessage")!;
            string addMapArea = Properties.Resources.GetString("OfflineMapAreasAddMapArea")!;
            string selectArea = Properties.Resources.GetString("OfflineMapAreasSelectArea")!;
            string cancel = Properties.Resources.GetString("OfflineMapAreasCancel")!;
            string areaNamePlaceholder = Properties.Resources.GetString("OfflineMapAreasAreaNamePlaceholder")!;
            string add = Properties.Resources.GetString("OfflineMapAreasAdd")!;

            Grid root = new Grid();

            // Main panel with list of map areas
            Grid mainView = new Grid()
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto),
                },
            };
            mainView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsAddOnDemandMode, BindingMode.OneWay, converter: InvertBoolConverter.Instance, source: RelativeBindingSource.TemplatedParent);
            CollectionView listView = new CollectionView()
            {
                SelectionMode = SelectionMode.None,
                ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 4 },
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
            };
            listView.SetBinding(ItemsView.VerticalScrollBarVisibilityProperty, static (OfflineMapAreasView view) => view.VerticalScrollBarVisibility, source: RelativeBindingSource.TemplatedParent);
            listView.SetBinding(ItemsView.ItemsSourceProperty, static (OfflineMapAreasView view) => view.TemplateSettings.MapAreas, source: RelativeBindingSource.TemplatedParent);
            listView.SetBinding(ItemsView.ItemTemplateProperty, static (OfflineMapAreasView view) => view.ItemTemplate, source: RelativeBindingSource.TemplatedParent);
            mainView.Children.Add(listView);

            VerticalStackLayout offlineDisabledView = CreateStateLayout(ToolkitIcons.ExclamationMarkTriangle, offlineDisabledTitle, offlineDisabledMessage);
            offlineDisabledView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.MapIsOfflineDisabled, source: RelativeBindingSource.TemplatedParent);
            mainView.Children.Add(offlineDisabledView);

            Button noInternetRefreshButton = CreateActionButton(refresh, ToolkitIcons.Refresh);
            VerticalStackLayout noInternetView = CreateStateLayout(ToolkitIcons.ExclamationMarkTriangle, noInternetConnectionTitle, noInternetConnectionMessage, noInternetRefreshButton);
            noInternetView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsInternetNotAvailable, source: RelativeBindingSource.TemplatedParent);
            mainView.Children.Add(noInternetView);

            Button refreshMapAreasButton = CreateActionButton(refresh, ToolkitIcons.Refresh);
            VerticalStackLayout noAreasView = CreateStateLayout(ToolkitIcons.DownloadTo, noMapAreasTitle, noMapAreasPreplannedMessage);
            noAreasView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.HasNoAreas, source: RelativeBindingSource.TemplatedParent);

            Label onDemandMessage = CreateStateMessage(noMapAreasOnDemandMessage);
            onDemandMessage.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsOnDemandMode, source: RelativeBindingSource.TemplatedParent);
            noAreasView.Children.Add(onDemandMessage);

            refreshMapAreasButton.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsPreplannedMode, source: RelativeBindingSource.TemplatedParent);
            noAreasView.Children.Add(refreshMapAreasButton);
            mainView.Children.Add(noAreasView);

            ActivityIndicator loadingIndicator = new ActivityIndicator() { IsVisible = false };
            loadingIndicator.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsLoadingModels, source: RelativeBindingSource.TemplatedParent);
            loadingIndicator.SetBinding(ActivityIndicator.IsRunningProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsLoadingModels, source: RelativeBindingSource.TemplatedParent);
            mainView.Children.Add(loadingIndicator);

            Button addMapAreaButton = CreateActionButton(addMapArea, ToolkitIcons.Plus);
            addMapAreaButton.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsOnDemandMode, source: RelativeBindingSource.TemplatedParent);
            Grid.SetRow(addMapAreaButton, 1);
            mainView.Children.Add(addMapAreaButton);
            root.Children.Add(mainView);

            // Add Map Area Panel
            Grid addAreaView = new Grid()
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star) { Height = 400 },
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                },
                RowSpacing = 8,
                VerticalOptions = LayoutOptions.Start,
            };
            addAreaView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsAddOnDemandMode, BindingMode.OneWay, source: RelativeBindingSource.TemplatedParent);

            Grid addAreaHeader = new Grid()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
            };
            Label addAreaTitle = new Label()
            {
                Text = selectArea,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            Grid.SetColumnSpan(addAreaTitle, 2);
            addAreaHeader.Children.Add(addAreaTitle);

            Button cancelAddOnDemandAreaButton = new Button()
            {
                Text = cancel,
                HorizontalOptions = LayoutOptions.End,
            };
            Grid.SetColumn(cancelAddOnDemandAreaButton, 1);
            addAreaHeader.Children.Add(cancelAddOnDemandAreaButton);
            addAreaView.Children.Add(addAreaHeader);

            Grid addAreaMapContainer = new Grid()
            {
                MaximumHeightRequest = 400,
            };
            MapView addAreaMapView = new MapView()
            {
                IsAttributionTextVisible = false,
                ViewInsets = new Thickness(40),
            };
            addAreaMapContainer.Children.Add(addAreaMapView);
            Border addAreaMapOverlay = new Border()
            {
                Stroke = Colors.Black,
                StrokeThickness = 40,
                Opacity = 0.5,
                InputTransparent = true,
            };
            addAreaMapContainer.Children.Add(addAreaMapOverlay);
            Grid.SetRow(addAreaMapContainer, 1);
            addAreaView.Children.Add(addAreaMapContainer);

            Entry addOnDemandAreaNameTextBox = new Entry()
            {
                Placeholder = areaNamePlaceholder,
            };
            Grid.SetRow(addOnDemandAreaNameTextBox, 2);
            addAreaView.Children.Add(addOnDemandAreaNameTextBox);

            Button acceptAddOnDemandAreaButton = new Button()
            {
                Text = add,
                HorizontalOptions = LayoutOptions.Fill,
            };
            Grid.SetRow(acceptAddOnDemandAreaButton, 3);
            addAreaView.Children.Add(acceptAddOnDemandAreaButton);
            root.Children.Add(addAreaView);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(ItemsViewName, listView);
            nameScope.RegisterName(RefreshMapAreasButtonName, refreshMapAreasButton);
            nameScope.RegisterName(NoInternetRefreshButtonName, noInternetRefreshButton);
            nameScope.RegisterName(AddMapAreaButtonName, addMapAreaButton);
            nameScope.RegisterName(AddAreaMapViewName, addAreaMapView);
            nameScope.RegisterName(AddOnDemandAreaNameTextBoxName, addOnDemandAreaNameTextBox);
            nameScope.RegisterName(AcceptAddOnDemandAreaButtonName, acceptAddOnDemandAreaButton);
            nameScope.RegisterName(CancelAddOnDemandAreaButtonName, cancelAddOnDemandAreaButton);
            return root;
        }

        private static object BuildMapAreasItemTemplate()
        {
            string downloaded = GetLocalizedString("OfflineMapAreasDownloaded");
            string open = GetLocalizedString("OfflineMapAreasOpen");

            Grid root = new Grid()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(68),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                },
                Margin = new Thickness(0, 4),
                MinimumHeightRequest = 64,
            };

            Border thumbnailBorder = new Border()
            {
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Margin = new Thickness(0, 0, 4, 0),
                WidthRequest = 64,
                HeightRequest = 64,
            };
            Image thumbnail = new Image()
            {
                WidthRequest = 64,
                HeightRequest = 64,
                Aspect = Aspect.AspectFill,
            };
            thumbnail.SetBinding(Image.SourceProperty, static (IOfflineMapAreaItem item) => item.ThumbnailData, converter: ImageSourceConverter);
            thumbnailBorder.Content = thumbnail;
            Grid.SetRowSpan(thumbnailBorder, 3);
            root.Children.Add(thumbnailBorder);

            Label title = new Label()
            {
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1,
            };
            title.SetBinding(Label.TextProperty, static (IOfflineMapAreaItem item) => item.Title);
            Grid.SetColumn(title, 1);
            root.Children.Add(title);

            Label description = new Label()
            {
                FontSize = 12,
                TextColor = Colors.Gray,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 3,
            };
            description.SetBinding(Label.TextProperty, static (IOfflineMapAreaItem item) => item.Description);
            Grid.SetColumn(description, 1);
            Grid.SetRow(description, 1);
            root.Children.Add(description);

            Label downloadedLabel = new Label()
            {
                Text = downloaded,
                FontSize = 12,
                TextColor = Colors.Gray,
                IsVisible = false,
            };
            downloadedLabel.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsDownloaded);
            Grid.SetColumn(downloadedLabel, 1);
            Grid.SetRow(downloadedLabel, 2);
            root.Children.Add(downloadedLabel);

            Grid actions = new Grid()
            {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
            };
            Grid.SetColumn(actions, 2);
            Grid.SetRowSpan(actions, 3);
            root.Children.Add(actions);

            CalciteImageButton stopButton = CreateIconButton(ToolkitIcons.CircleStop);
            stopButton.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsDownloading);
            stopButton.SetBinding(ImageButton.CommandProperty, static (IOfflineMapAreaItem item) => item.StopDownloadCommand);
            actions.Children.Add(stopButton);

            CalciteImageButton downloadButton = CreateIconButton(ToolkitIcons.DownloadTo);
            downloadButton.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.AllowsDownload);
            downloadButton.SetBinding(ImageButton.CommandProperty, static (IOfflineMapAreaItem item) => item.DownloadCommand);
            actions.Children.Add(downloadButton);

            ProgressBar progressBar = new ProgressBar() { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.End };
            progressBar.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsDownloading);
            progressBar.SetBinding(ProgressBar.ProgressProperty, static (IOfflineMapAreaItem item) => item.DownloadProgress);
            Grid.SetColumn(progressBar, 1);
            Grid.SetColumnSpan(progressBar, 2);
            Grid.SetRow(progressBar, 2);
            root.Children.Add(progressBar);
            
            HorizontalStackLayout downloadedActions = new HorizontalStackLayout()
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false,
            };
            downloadedActions.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsDownloaded);
            CalciteImageButton removeButton = CreateIconButton(ToolkitIcons.Trash);
            removeButton.SetBinding(ImageButton.CommandProperty, static (IOfflineMapAreaItem item) => item.RemoveDownloadCommand);
            downloadedActions.Children.Add(removeButton);

            Button openButton = new Button() { Text = open };
            openButton.SetBinding(Button.CommandProperty, static (IOfflineMapAreaItem item) => item.OpenCommand);
            openButton.SetBinding(Button.CommandParameterProperty, static (IOfflineMapAreaItem item) => item.Map);
            downloadedActions.Children.Add(openButton);
            actions.Children.Add(downloadedActions);

            CalciteImageButton failedButton = CreateIconButton(ToolkitIcons.ExclamationMarkCircle);
            failedButton.SetBinding(ToolTipProperties.TextProperty, static (IOfflineMapAreaItem item) => item.Error?.Message);
            failedButton.SetBinding(SemanticProperties.DescriptionProperty, static (IOfflineMapAreaItem item) => item.Error?.Message);
            failedButton.SetBinding(Button.CommandProperty, static (IOfflineMapAreaItem item) => item.RemoveDownloadCommand);
            failedButton.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.Error, converter: EmptyToFalseConverter.Instance);
            downloadedActions.Children.Add(failedButton);

            actions.Children.Add(downloadedActions);

            return root;
        }

        private OfflineMapAreasTemplateSettings TemplateSettings { get; } = new OfflineMapAreasTemplateSettings();

        private static VerticalStackLayout CreateStateLayout(string icon, string title, string message, Button? actionButton = null)
        {
            VerticalStackLayout layout = new VerticalStackLayout()
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(24),
            };

            Label iconLabel = new Label()
            {
                Text = icon,
                FontFamily = ToolkitIcons.FontFamilyName,
                FontSize = 48,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Colors.Gray,
            };
            layout.Children.Add(iconLabel);

            Label titleLabel = new Label()
            {
                Text = title,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            layout.Children.Add(titleLabel);
            layout.Children.Add(CreateStateMessage(message));

            if (actionButton is not null)
            {
                layout.Children.Add(actionButton);
            }

            return layout;
        }

        private static Label CreateStateMessage(string message) => new Label()
        {
            Text = message,
            FontSize = 12,
            TextColor = Colors.Gray,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap,
        };

        private static Button CreateActionButton(string text, string glyph) => new Button()
        {
            Text = text,
            ImageSource = new FontImageSource
            {
                Glyph = glyph,
                FontFamily = ToolkitIcons.FontFamilyName,
                Size = 16,
            },
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 4),
            HorizontalOptions = LayoutOptions.Center,
        };

        private static CalciteImageButton CreateIconButton(string glyph) => new CalciteImageButton(glyph, 16)
        {
            WidthRequest = 32,
            HeightRequest = 32,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            Padding = 0,
        };

        private void OnApplyTemplateMaui()
        {
            if (_refreshMapAreasButton is not null)
            {
                _refreshMapAreasButton.Clicked -= RefreshMapAreasButton_Clicked;
            }

            if (_noInternetRefreshButton is not null)
            {
                _noInternetRefreshButton.Clicked -= RefreshMapAreasButton_Clicked;
            }

            if (_addMapAreaButton is not null)
            {
                _addMapAreaButton.Clicked -= AddMapAreaButton_Clicked;
            }

            if (_acceptAddOnDemandAreaButton is not null)
            {
                _acceptAddOnDemandAreaButton.Clicked -= AcceptAddOnDemandAreaButton_Clicked;
            }

            if (_cancelAddOnDemandAreaButton is not null)
            {
                _cancelAddOnDemandAreaButton.Clicked -= CancelAddOnDemandAreaButton_Clicked;
            }

            _refreshMapAreasButton = GetTemplateChild(RefreshMapAreasButtonName) as Button;
            _noInternetRefreshButton = GetTemplateChild(NoInternetRefreshButtonName) as Button;
            _addMapAreaButton = GetTemplateChild(AddMapAreaButtonName) as Button;
            _acceptAddOnDemandAreaButton = GetTemplateChild(AcceptAddOnDemandAreaButtonName) as Button;
            _cancelAddOnDemandAreaButton = GetTemplateChild(CancelAddOnDemandAreaButtonName) as Button;

            if (_refreshMapAreasButton is not null)
            {
                _refreshMapAreasButton.Clicked += RefreshMapAreasButton_Clicked;
            }

            if (_noInternetRefreshButton is not null)
            {
                _noInternetRefreshButton.Clicked += RefreshMapAreasButton_Clicked;
            }

            if (_addMapAreaButton is not null)
            {
                _addMapAreaButton.Clicked += AddMapAreaButton_Clicked;
            }

            if (_acceptAddOnDemandAreaButton is not null)
            {
                _acceptAddOnDemandAreaButton.Clicked += AcceptAddOnDemandAreaButton_Clicked;
            }

            if (_cancelAddOnDemandAreaButton is not null)
            {
                _cancelAddOnDemandAreaButton.Clicked += CancelAddOnDemandAreaButton_Clicked;
            }
        }

        private void RefreshMapAreasButton_Clicked(object? sender, EventArgs e) => _ = _vm?.LoadModelsAsync();

        private void AddMapAreaButton_Clicked(object? sender, EventArgs e) => InitAddOnDemandArea();

        private void AcceptAddOnDemandAreaButton_Clicked(object? sender, EventArgs e) => AddOnDemandArea();

        private void CancelAddOnDemandAreaButton_Clicked(object? sender, EventArgs e) => CloseAddOnDemandArea();
    }
}
#endif