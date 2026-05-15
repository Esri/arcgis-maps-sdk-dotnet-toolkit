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

        private Button? _refreshMapAreasButton;
        private Button? _noInternetRefreshButton;

        static OfflineMapAreasView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
            DefaultItemTemplate = new DataTemplate(BuildMapAreasItemTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Grid root = new Grid()
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto),
                },
            };

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
            root.Children.Add(listView);

            VerticalStackLayout offlineDisabledView = CreateStateLayout(ToolkitIcons.ExclamationMarkTriangle, "Offline Disabled", "The map is not enabled for offline use");
            offlineDisabledView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.MapIsOfflineDisabled, source: RelativeBindingSource.TemplatedParent);
            root.Children.Add(offlineDisabledView);

            Button noInternetRefreshButton = CreateActionButton("Refresh", ToolkitIcons.Refresh);
            VerticalStackLayout noInternetView = CreateStateLayout(ToolkitIcons.ExclamationMarkTriangle, "No Internet Connection", "Could not retrieve map areas for this map", noInternetRefreshButton);
            noInternetView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsInternetNotAvailable, source: RelativeBindingSource.TemplatedParent);
            root.Children.Add(noInternetView);

            Button refreshMapAreasButton = CreateActionButton("Refresh", ToolkitIcons.Refresh);
            VerticalStackLayout noAreasView = CreateStateLayout(ToolkitIcons.DownloadTo, "No Map Areas", "There are no map areas for this map.");
            noAreasView.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.HasNoAreas, source: RelativeBindingSource.TemplatedParent);

            Label onDemandMessage = CreateStateMessage("There are no map areas for this map. Tap the button below to get started.");
            onDemandMessage.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsOnDemandMode, source: RelativeBindingSource.TemplatedParent);
            noAreasView.Children.Add(onDemandMessage);

            refreshMapAreasButton.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsPreplannedMode, source: RelativeBindingSource.TemplatedParent);
            noAreasView.Children.Add(refreshMapAreasButton);
            root.Children.Add(noAreasView);

            ActivityIndicator loadingIndicator = new ActivityIndicator() { IsVisible = false };
            loadingIndicator.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsLoadingModels, source: RelativeBindingSource.TemplatedParent);
            loadingIndicator.SetBinding(ActivityIndicator.IsRunningProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsLoadingModels, source: RelativeBindingSource.TemplatedParent);
            root.Children.Add(loadingIndicator);

            Button addMapAreaButton = CreateActionButton("Add Map Area", ToolkitIcons.Plus);
            addMapAreaButton.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.IsOnDemandMode, source: RelativeBindingSource.TemplatedParent);
            Grid.SetRow(addMapAreaButton, 1);
            root.Children.Add(addMapAreaButton);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(ItemsViewName, listView);
            nameScope.RegisterName(RefreshMapAreasButtonName, refreshMapAreasButton);
            nameScope.RegisterName(NoInternetRefreshButtonName, noInternetRefreshButton);
            nameScope.RegisterName(AddMapAreaButtonName, addMapAreaButton);
            return root;
        }

        private static object BuildMapAreasItemTemplate()
        {
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
                Text = "Downloaded",
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

            CalciteImageButton stopButton = CreateIconButton(ToolkitIcons.X);
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

            Button openButton = new Button() { Text = "Open" };
            openButton.SetBinding(Button.CommandProperty, static (IOfflineMapAreaItem item) => item.OpenCommand);
            openButton.SetBinding(Button.CommandParameterProperty, static (IOfflineMapAreaItem item) => item.Map);
            downloadedActions.Children.Add(openButton);
            actions.Children.Add(downloadedActions);

            return root;
        }

        private OfflineMapAreasTemplateSettings TemplateSettings { get; set; }

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

            _refreshMapAreasButton = GetTemplateChild(RefreshMapAreasButtonName) as Button;
            _noInternetRefreshButton = GetTemplateChild(NoInternetRefreshButtonName) as Button;

            if (_refreshMapAreasButton is not null)
            {
                _refreshMapAreasButton.Clicked += RefreshMapAreasButton_Clicked;
            }

            if (_noInternetRefreshButton is not null)
            {
                _noInternetRefreshButton.Clicked += RefreshMapAreasButton_Clicked;
            }
        }

        private void RefreshMapAreasButton_Clicked(object? sender, EventArgs e) => _ = _vm?.LoadModelsAsync();
    }
}
#endif