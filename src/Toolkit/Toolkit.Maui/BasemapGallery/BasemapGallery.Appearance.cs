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

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class BasemapGallery : TemplatedView
{
    private const double ViewStyleWidthThreshold = 384.0;

    // Tracks currently-applied layout to avoid unnecessary re-styling of the view
    private int _currentSelectedSpan = 0;
    private BasemapGalleryViewStyle _currentlyAppliedViewStyle = BasemapGalleryViewStyle.Automatic;

    private View? _loadingScrim;

    private static readonly DataTemplate DefaultListDataTemplate;
    private static readonly DataTemplate DefaultGridDataTemplate;
    private static readonly ControlTemplate DefaultControlTemplate;
    private static readonly BoolToOpacityConverter OpacityConverter;
    private static readonly ByteArrayToImageSourceConverter ImageSourceConverter;

    static BasemapGallery()
    {
        OpacityConverter = new BoolToOpacityConverter();
        ImageSourceConverter = new ByteArrayToImageSourceConverter();

        DefaultGridDataTemplate = new DataTemplate(() =>
        {
            Border border = new Border { Padding = 4, StrokeThickness = 1, StrokeShape = new Rectangle() };
            Grid outerScrimContainer = new Grid();
            outerScrimContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(86) });
            outerScrimContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            outerScrimContainer.RowSpacing = 4;

            Image thumbnail = new Image { WidthRequest = 64, HeightRequest = 64, Aspect = Aspect.AspectFill, BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Center };
            Label nameLabel = new Label { FontSize = 12, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Start };

            outerScrimContainer.Children.Add(thumbnail);
            outerScrimContainer.Children.Add(nameLabel);

            Grid.SetRow(thumbnail, 0);
            Grid.SetRow(nameLabel, 1);


            Grid scrimGrid = new Grid();
            scrimGrid.SetAppThemeColor(BackgroundColorProperty, Colors.White, Colors.Black);
            outerScrimContainer.Children.Add(scrimGrid);
            Grid.SetRowSpan(scrimGrid, 2);


            thumbnail.SetBinding(Image.SourceProperty, nameof(BasemapGalleryItem.ThumbnailData), converter: ImageSourceConverter);
            nameLabel.SetBinding(Label.TextProperty, nameof(BasemapGalleryItem.Name));
            scrimGrid.SetBinding(OpacityProperty, nameof(BasemapGalleryItem.IsValid), mode: BindingMode.OneWay, converter: OpacityConverter);

            border.Content = outerScrimContainer;
            return border;
        });

        DefaultListDataTemplate = new DataTemplate(() =>
        {
            // Special template to take advantate of negative margin support on iOS, Mac
#if __IOS__
            Border border = new Border { Padding = new Thickness(0), StrokeThickness = 8, StrokeShape = new Rectangle() };
            Grid outerScrimContainer = new Grid();
            outerScrimContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            outerScrimContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            outerScrimContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            outerScrimContainer.ColumnSpacing = 0;
            outerScrimContainer.Margin = new Thickness(-4, -8, -8, -8);
            outerScrimContainer.SetBinding(BackgroundColorProperty, new Binding { Source = border, Path = nameof(border.BackgroundColor) });

            Image thumbnail = new Image { WidthRequest = 64, HeightRequest = 64, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Start };
            Label nameLabel = new Label { FontSize = 12, HorizontalTextAlignment = TextAlignment.Start, VerticalTextAlignment = TextAlignment.Center };
            thumbnail.SetBinding(BackgroundColorProperty, new Binding { Source = border, Path = nameof(border.BackgroundColor) });

            outerScrimContainer.Children.Add(thumbnail);
            outerScrimContainer.Children.Add(nameLabel);

            Grid.SetColumn(thumbnail, 0);
            Grid.SetColumn(nameLabel, 2);


            Grid scrimGrid = new Grid();
            scrimGrid.SetAppThemeColor(BackgroundColorProperty, Colors.White, Colors.Black);
            outerScrimContainer.Children.Add(scrimGrid);
            Grid.SetColumnSpan(scrimGrid, 3);


            thumbnail.SetBinding(Image.SourceProperty, nameof(BasemapGalleryItem.ThumbnailData), converter: ImageSourceConverter);
            nameLabel.SetBinding(Label.TextProperty, nameof(BasemapGalleryItem.Name));
            scrimGrid.SetBinding(OpacityProperty, nameof(BasemapGalleryItem.IsValid), mode: BindingMode.OneWay, converter: OpacityConverter);

            border.Content = outerScrimContainer;
            return border;
#else
            Border border = new Border { Padding = new Thickness(0), StrokeThickness = 1, StrokeShape = new Rectangle() };
            Grid outerScrimContainer = new Grid();
            outerScrimContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            outerScrimContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            outerScrimContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            outerScrimContainer.ColumnSpacing = 0;

            Image thumbnail = new Image { WidthRequest = 64, HeightRequest = 64, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Start };
            Label nameLabel = new Label { FontSize = 12, HorizontalTextAlignment = TextAlignment.Start, VerticalTextAlignment = TextAlignment.Center };

            outerScrimContainer.Children.Add(thumbnail);
            outerScrimContainer.Children.Add(nameLabel);

            Grid.SetColumn(thumbnail, 0);
            Grid.SetColumn(nameLabel, 2);


            Grid scrimGrid = new Grid();
            scrimGrid.SetAppThemeColor(BackgroundColorProperty, Colors.White, Colors.Black);
            outerScrimContainer.Children.Add(scrimGrid);
            Grid.SetColumnSpan(scrimGrid, 3);


            thumbnail.SetBinding(Image.SourceProperty, nameof(BasemapGalleryItem.ThumbnailData), converter: ImageSourceConverter);
            nameLabel.SetBinding(Label.TextProperty, nameof(BasemapGalleryItem.Name));
            scrimGrid.SetBinding(OpacityProperty, nameof(BasemapGalleryItem.IsValid), mode: BindingMode.OneWay, converter: OpacityConverter);

            border.Content = outerScrimContainer;
            return border;
#endif
        });

        string template = $@"<ControlTemplate xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui"">
                                    <Grid>
                                            <Grid.Resources>
                                                 <Style TargetType=""Border"">
                                                        <Setter Property=""VisualStateManager.VisualStateGroups"">
                                                            <VisualStateGroupList>
                                                                <VisualStateGroup x:Name=""CommonStates"">
                                                                    <VisualState x:Name=""Normal"">
                                                                        <VisualState.Setters>
                                                                            <Setter Property=""BackgroundColor""
                                                                                    Value=""{{AppThemeBinding Light=#fff,Dark=#353535}}"" />
                                                                            <Setter Property=""Stroke""
                                                                                    Value=""{{AppThemeBinding Light=#fff,Dark=#353535}}"" />
                                                                        </VisualState.Setters>
                                                                    </VisualState>
                                                                    <VisualState x:Name=""Selected"">
                                                                        <VisualState.Setters>
                                                                            <Setter Property=""BackgroundColor""
                                                                                    Value=""{{AppThemeBinding Light=#e2f1fb,Dark=#202020}}"" />
                                                                            <Setter Property=""Stroke""
                                                                                    Value=""{{AppThemeBinding Light=#007ac2,Dark=#009af2}}"" />
                                                                        </VisualState.Setters>
                                                                    </VisualState>
                                                                </VisualStateGroup>
                                                            </VisualStateGroupList>
                                                        </Setter>
                                                    </Style>
                                                    <Style TargetType=""Label"">
                                                        <Setter Property=""TextColor"" Value=""{{AppThemeBinding Light=#6a6a6a,Dark=#fff}}"" />
                                                    </Style>
                                                </Grid.Resources>
                                        <CollectionView x:Name=""PART_InnerListView"" HorizontalOptions=""Fill"" VerticalOptions=""Fill"" SelectionMode=""Single"" BackgroundColor=""{{AppThemeBinding Light=#fff,Dark=#353535}}"" />
                                        <Grid x:Name=""PART_LoadingScrim"" IsVisible=""False"">
                                            <Grid BackgroundColor=""{{AppThemeBinding Light=White, Dark=Black}}"" Opacity=""0.3"" />
                                            <ActivityIndicator IsRunning=""True"" HorizontalOptions=""Center"" VerticalOptions=""Center"" />
                                        </Grid>
                                    </Grid>
                                </ControlTemplate>";
        DefaultControlTemplate = new ControlTemplate().LoadFromXaml(template);
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        ListView = GetTemplateChild("PART_InnerListView") as CollectionView;
        VisualStateManager.SetVisualStateGroups(ListView, new VisualStateGroupList());
        _loadingScrim = GetTemplateChild("PART_LoadingScrim") as View;
        if (ListView != null)
        {
            ListView.BindingContext = _controller;
            ListView.ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Vertical);
        }
    }

    /// <summary>
    /// Gets or sets the data template used to show basemaps in a list.
    /// </summary>
    /// <seealso cref="GalleryViewStyle"/>
    public DataTemplate? ListItemTemplate
    {
        get => GetValue(ListItemTemplateProperty) as DataTemplate;
        set => SetValue(ListItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the template used to show basemaps in a grid.
    /// </summary>
    /// <seealso cref="GalleryViewStyle"/>
    public DataTemplate? GridItemTemplate
    {
        get => GetValue(GridItemTemplateProperty) as DataTemplate;
        set => SetValue(GridItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the view style for the gallery.
    /// </summary>
    public BasemapGalleryViewStyle GalleryViewStyle
    {
        get => (BasemapGalleryViewStyle)GetValue(GalleryViewStyleProperty);
        set => SetValue(GalleryViewStyleProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ListItemTemplate"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ListItemTemplateProperty =
        BindableProperty.Create(nameof(ListItemTemplate), typeof(DataTemplate), typeof(BasemapGallery), null, BindingMode.OneWay, null, propertyChanged: ItemTemplateChanged);

    /// <summary>
    /// Identifies the <see cref="GridItemTemplate"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GridItemTemplateProperty =
        BindableProperty.Create(nameof(GridItemTemplate), typeof(DataTemplate), typeof(BasemapGallery), null, BindingMode.OneWay, null, propertyChanged: ItemTemplateChanged);

    /// <summary>
    /// Identifies the <see cref="GalleryViewStyle"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GalleryViewStyleProperty =
        BindableProperty.Create(nameof(GalleryViewStyle), typeof(BasemapGalleryViewStyle), typeof(BasemapGallery), BasemapGalleryViewStyle.Automatic, BindingMode.OneWay, propertyChanged: ItemTemplateChanged);

    /// <summary>
    /// Handles property changes for the bindable properties that can trigger a style or template change.
    /// </summary>
    private static void ItemTemplateChanged(BindableObject sender, object oldValue, object newValue)
    {
        BasemapGallery sendingView = (BasemapGallery)sender;
        sendingView.HandleTemplateChange(sendingView.Width);
    }

    /// <summary>
    /// Updates the view based on the current width, adjusting the collection view presentation style as configured.
    /// </summary>
    /// <seealso cref="GridItemTemplate"/>
    /// <seealso cref="ListItemTemplate"/>
    /// <seealso cref="GalleryViewStyle"/>
    private void HandleTemplateChange(double currentWidth)
    {
        if (ListView == null)
        {
            return;
        }

        BasemapGalleryViewStyle styleAfterUpdate = BasemapGalleryViewStyle.Automatic;
        int gridSpanAfterUpdate = 0;
        switch (GalleryViewStyle)
        {
            case BasemapGalleryViewStyle.List:
                styleAfterUpdate = BasemapGalleryViewStyle.List;
                break;
            case BasemapGalleryViewStyle.Grid:
                styleAfterUpdate = BasemapGalleryViewStyle.Grid;
                break;
            case BasemapGalleryViewStyle.Automatic:
                styleAfterUpdate = currentWidth >= ViewStyleWidthThreshold ? BasemapGalleryViewStyle.Grid : BasemapGalleryViewStyle.List;
                break;
        }

        if (styleAfterUpdate == BasemapGalleryViewStyle.Grid)
        {
            gridSpanAfterUpdate = System.Math.Max((int)(currentWidth / 128), 1);
        }

        if (_currentSelectedSpan != gridSpanAfterUpdate || styleAfterUpdate != _currentlyAppliedViewStyle)
        {
            if (styleAfterUpdate == BasemapGalleryViewStyle.List)
            {
                ListView.ItemTemplate = ListItemTemplate;
                UpdateItemsLayout(1, 0, 0);
                ListView.Margin = new Thickness(0);
            }
            else
            {
                ListView.ItemTemplate = DefaultGridDataTemplate;
                UpdateItemsLayout(gridSpanAfterUpdate, 4, 4);
                ListView.Margin = new Thickness(4, 4, 0, 0);
            }

            _currentSelectedSpan = gridSpanAfterUpdate;
            _currentlyAppliedViewStyle = styleAfterUpdate;
        }
    }

    private void UpdateItemsLayout(int span, double verticalSpacing, double horizontalSpacing)
    {
        if (ListView is not null)
        {
#if __IOS__
            // This is a workaround for a bug in the current version of the iOS renderer for CollectionView
            // where CollectionView throws `NullReferneceException` on changing span of GridItemsLayout.
            // It won't be needed once we move MAUI version up to 8.0.10+.
            // Will have to consider if toolkit can require a newer version (currently it's fixed to API).
            ListView.ItemsLayout = new GridItemsLayout(span, ItemsLayoutOrientation.Vertical)
            {
                VerticalItemSpacing = verticalSpacing,
                HorizontalItemSpacing = horizontalSpacing,
            }; 
#else
            if (ListView.ItemsLayout is GridItemsLayout layout)
            {
                layout.Span = span;
                layout.VerticalItemSpacing = verticalSpacing;
                layout.HorizontalItemSpacing = horizontalSpacing;
            }
#endif
        }
    }

    /// <inheritdoc />
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // Size change could necesitate a switch between list and grid presentations.
        HandleTemplateChange(width);
    }
}