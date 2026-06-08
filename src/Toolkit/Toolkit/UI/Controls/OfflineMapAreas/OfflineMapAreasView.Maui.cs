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
using Microsoft.Maui.Layouts;

namespace Esri.ArcGISRuntime.Toolkit.Maui
{
    public partial class OfflineMapAreasView : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly ByteArrayToImageSourceConverter ImageSourceConverter = new();

        private const string ItemsViewName = "MapAreasView";
        private const string RefreshMapAreasButtonName = "RefreshMapAreasButton";
        private const string NoInternetRefreshButtonName = "NoInternetRefreshButton";
        private const string AddMapAreaButtonName = "AddMapAreaButton";
        private const string AddAreaMapViewName = "AddAreaMapView";
        private const string AddOnDemandAreaScaleSelectorName = "AddOnDemandAreaScaleSelector";
        private const string AddOnDemandAreaNameTextBoxName = "AddOnDemandAreaNameTextBox";
        private const string AcceptAddOnDemandAreaButtonName = "AcceptAddOnDemandAreaButton";
        private const string CancelAddOnDemandAreaButtonName = "CancelAddOnDemandAreaButton";

        static OfflineMapAreasView()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
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
            string scale = Properties.Resources.GetString("OfflineMapAreasScaleLabel")!;
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
            addMapAreaButton.SetBinding(IsVisibleProperty, static (OfflineMapAreasView view) => view.TemplateSettings.CanAddMapArea, source: RelativeBindingSource.TemplatedParent);
            Grid.SetRow(addMapAreaButton, 1);
            mainView.Children.Add(addMapAreaButton);
            root.Children.Add(mainView);

            // Add Map Area Panel
            var addAreaView = new StretchStackPanel()
            {
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
                MinimumHeightRequest = 100,
            };
            MapView addAreaMapView = new MapView()
            {
                IsAttributionTextVisible = false,
                ViewInsets = new Thickness(20),
            };
            addAreaMapContainer.Children.Add(addAreaMapView);
            Border addAreaMapOverlay = new Border()
            {
                Stroke = Colors.Black,
                StrokeThickness = 20,
                Opacity = 0.5,
                InputTransparent = true,
            };
            addAreaMapContainer.Children.Add(addAreaMapOverlay);
            StretchStackPanel.SetGridLength(addAreaMapContainer, new GridLength(1, GridUnitType.Star));
            addAreaView.Children.Add(addAreaMapContainer);

            Label scaleLabel = new Label()
            {
                Text = scale,
            };
            addAreaView.Children.Add(scaleLabel);

            Picker addOnDemandAreaScaleSelector = new Picker();
            addAreaView.Children.Add(addOnDemandAreaScaleSelector);

            Entry addOnDemandAreaNameTextBox = new Entry()
            {
                Placeholder = areaNamePlaceholder,
            };
            addAreaView.Children.Add(addOnDemandAreaNameTextBox);

            Button acceptAddOnDemandAreaButton = new Button()
            {
                Text = add,
                HorizontalOptions = LayoutOptions.Fill,
            };
            addAreaView.Children.Add(acceptAddOnDemandAreaButton);
            root.Children.Add(addAreaView);

            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName(ItemsViewName, listView);
            nameScope.RegisterName(RefreshMapAreasButtonName, refreshMapAreasButton);
            nameScope.RegisterName(NoInternetRefreshButtonName, noInternetRefreshButton);
            nameScope.RegisterName(AddMapAreaButtonName, addMapAreaButton);
            nameScope.RegisterName(AddAreaMapViewName, addAreaMapView);
            nameScope.RegisterName(AddOnDemandAreaScaleSelectorName, addOnDemandAreaScaleSelector);
            nameScope.RegisterName(AddOnDemandAreaNameTextBoxName, addOnDemandAreaNameTextBox);
            nameScope.RegisterName(AcceptAddOnDemandAreaButtonName, acceptAddOnDemandAreaButton);
            nameScope.RegisterName(CancelAddOnDemandAreaButtonName, cancelAddOnDemandAreaButton);
            return root;
        }

        private static object BuildMapAreasItemTemplate()
        {
            Grid root = new Grid()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(84),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                },
                HeightRequest = 79,
            };

            Border thumbnailBorder = new Border()
            {
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Margin = new Thickness(0, 0, 4, 0),
                WidthRequest = 80,
                HeightRequest = 80,
            };
            Image thumbnail = new Image()
            {
                WidthRequest = 80,
                HeightRequest = 80,
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
                Text = Properties.Resources.GetString("OfflineMapAreasDownloaded"),
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

            CalciteImageButton stopButton = CreateIconButton(ToolkitIcons.CircleStop, Properties.Resources.GetString("OfflineMapAreasStop"));
            stopButton.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsDownloading);
            stopButton.SetBinding(ImageButton.CommandProperty, static (IOfflineMapAreaItem item) => item.StopDownloadCommand);
            actions.Children.Add(stopButton);

            CalciteImageButton downloadButton = CreateIconButton(ToolkitIcons.DownloadTo, Properties.Resources.GetString("OfflineMapAreasDownload"));
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

            CalciteImageButton failedButton = CreateIconButton(ToolkitIcons.ExclamationMarkCircle, null);
            failedButton.SetBinding(ToolTipProperties.TextProperty, static (IOfflineMapAreaItem item) => item.Error?.Message);
            failedButton.SetBinding(SemanticProperties.HintProperty, static (IOfflineMapAreaItem item) => item.Error?.Message);
            failedButton.SetBinding(Button.CommandProperty, static (IOfflineMapAreaItem item) => item.RemoveDownloadCommand);
            failedButton.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.Error, converter: EmptyToFalseConverter.Instance);
            actions.Children.Add(failedButton);

            HorizontalStackLayout downloadedActions = new HorizontalStackLayout()
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false,
            };
            downloadedActions.SetBinding(IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsDownloaded);

            CalciteImageButton removeButton = CreateIconButton(ToolkitIcons.Trash, Properties.Resources.GetString("OfflineMapAreasRemove"));
            removeButton.SetBinding(ImageButton.CommandProperty, static (IOfflineMapAreaItem item) => item.RemoveDownloadCommand);
            removeButton.SetBinding(Button.IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsOpen, converter: InvertBoolConverter.Instance);
            downloadedActions.Children.Add(removeButton);

            Button openButton = new Button() { Text = Properties.Resources.GetString("OfflineMapAreasOpen") };
            openButton.SetBinding(Button.CommandProperty, static (IOfflineMapAreaItem item) => item.OpenCommand);
            openButton.SetBinding(Button.CommandParameterProperty, static (IOfflineMapAreaItem item) => item.Map);
            openButton.SetBinding(Button.IsVisibleProperty, static (IOfflineMapAreaItem item) => item.IsOpen, converter: InvertBoolConverter.Instance);
            downloadedActions.Children.Add(openButton);
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

        private static CalciteImageButton CreateIconButton(string glyph, string? hint)
        {
            var button = new CalciteImageButton(glyph, 16)
            {
                WidthRequest = 32,
                HeightRequest = 32,
                BackgroundColor = Colors.Transparent,
                BorderWidth = 0,
                Padding = 0,
            };
            if (!string.IsNullOrEmpty(hint))
            {
                SemanticProperties.SetDescription(button, hint);
                ToolTipProperties.SetText(button, hint);
            }
            return button;
        }



        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild(RefreshMapAreasButtonName) is Button refreshMapAreasButton)
            {
                refreshMapAreasButton.Clicked += (s,e) =>  _ = _vm?.LoadModelsAsync();
            }

            if (GetTemplateChild(NoInternetRefreshButtonName) is Button noInternetRefreshButton)
            {
                noInternetRefreshButton.Clicked += (s,e) => _vm?.LoadModelsAsync();
            }

            if (GetTemplateChild(AddMapAreaButtonName) is Button addMapAreaButton)
            {
                addMapAreaButton.Clicked +=  (s,e) => InitAddOnDemandArea();
            }

            if (GetTemplateChild(AcceptAddOnDemandAreaButtonName) is Button acceptAddOnDemandAreaButton)
            {
                acceptAddOnDemandAreaButton.Clicked += (s, e) => AddOnDemandArea();
            }

            if (GetTemplateChild(CancelAddOnDemandAreaButtonName) is Button cancelAddOnDemandAreaButton)
            {
                cancelAddOnDemandAreaButton.Clicked += (s, e) => CloseAddOnDemandArea();
            }
        }

        private partial class StretchStackPanel : Layout
        {
            const double _padding = 8;
            public StretchStackPanel()
            {
                VerticalOptions = LayoutOptions.Start;
                HorizontalOptions = LayoutOptions.Fill;
            }
            protected override ILayoutManager CreateLayoutManager()
            {
                return new LayoutManager(this);
            }

            private class LayoutManager : ILayoutManager
            {
                private StretchStackPanel Layout { get; }
                public LayoutManager(StretchStackPanel layout)
                {
                    Layout = layout;
                }

                public Size Measure(double widthConstraint, double heightConstraint)
                {
                    double height = 0;
                    bool hasStarSizing = false;
                    for (int n = 0; n < Layout.Count; n++)
                    {
                        var child = Layout[n];

                        if (child.Visibility == Visibility.Collapsed)
                        {
                            continue;
                        }
                        if (height > 0)
                            height += _padding;
                        hasStarSizing = hasStarSizing || StretchStackPanel.GetGridLength(child as BindableObject).IsStar;
                        var size = child.Measure(widthConstraint, heightConstraint);
                        height += size.Height;
                    }
                    if (hasStarSizing)
                        return new Size(widthConstraint, heightConstraint);
                    return new Size(widthConstraint, Math.Min(heightConstraint, height));
                }

                public Size ArrangeChildren(Rect bounds)
                {
                    double ratio = 0;
                    double height = 0;

                   for (int n = 0; n < Layout.Count; n++)
                    {
                        var child = Layout[n];

                        if (child.Visibility == Visibility.Collapsed)
                        {
                            continue;
                        }
                        var value = StretchStackPanel.GetGridLength(child as BindableObject);
                        if (height > 0)
                            height += _padding;
                        if (value.IsStar)
                        {
                            ratio += value.Value;
                            var min = (child as VisualElement)?.MinimumHeightRequest ?? 0;
                            if (min > 0)
                                height += min;
                        }
                        else
                            height += child.DesiredSize.Height;
                    }

                    var leftover = Math.Max(0, bounds.Height - height);
                    double y = 0;
                    for (int n = 0; n < Layout.Count; n++)
                    {
                        var child = Layout[n];
                        if (child.Visibility == Visibility.Collapsed)
                        {
                            continue;
                        }
                        if (y > 0)
                            y += _padding;
                        var childHeight = child.DesiredSize.Height;
                        var value = StretchStackPanel.GetGridLength(child as BindableObject);
                        if (value.IsStar)
                        {
                            var h = leftover / ratio * value.Value;
                            var min = (child as VisualElement)?.MinimumHeightRequest ?? 0;
                            var max = (child as VisualElement)?.MaximumHeightRequest ?? double.MaxValue;
                            childHeight = Math.Clamp(h, min, max);
                        }
                        child.Arrange(new Rect(0, y, bounds.Width, childHeight));
                        y += childHeight;
                    }
                    return new Size(bounds.Width, y);
                }
            }
            public static readonly BindableProperty SizeProperty = BindableProperty.CreateAttached("Size",
                typeof(GridLength), typeof(Grid), GridLength.Auto, propertyChanged: Invalidate);

            public static GridLength GetGridLength(BindableObject? bindable) => bindable is null ? GridLength.Auto : (GridLength)bindable.GetValue(SizeProperty);
            public static void SetGridLength(BindableObject bindable, GridLength value) => bindable.SetValue(SizeProperty, value);

            private static void Invalidate(BindableObject bindable, object oldValue, object newValue)
            {
                if (bindable is IView view)
                    view.InvalidateMeasure();
            }
        }
    }
}
#endif