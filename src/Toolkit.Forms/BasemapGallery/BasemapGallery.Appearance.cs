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
using Esri.ArcGISRuntime.Toolkit.UI;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
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

        private static readonly TapGestureRecognizer tapGestureRecognizer;

        static BasemapGallery()
        {
            OpacityConverter = new BoolToOpacityConverter();

            DefaultGridDataTemplate = new DataTemplate(() =>
            {
                Grid outerScrimContainer = new Grid();
                outerScrimContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = 128 });

                StackLayout parentLayout = new StackLayout() { Orientation = StackOrientation.Vertical };
                parentLayout.Padding = new Thickness(8);
                Grid imageContainer = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                Image fallback = new Image { WidthRequest = 32, HeightRequest = 32, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                fallback.Source = ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Assets.BasemapLight.png", typeof(BasemapGallery).Assembly);
                Image thumbnail = new Image { WidthRequest = 64, HeightRequest = 64, Aspect = Aspect.AspectFill };
                Label nameLabel = new Label { FontSize = 11, TextColor = Color.FromHex("#6e6e6e"), HorizontalTextAlignment = TextAlignment.Center };
                imageContainer.Children.Add(fallback);
                imageContainer.Children.Add(thumbnail);
                parentLayout.Children.Add(imageContainer);
                parentLayout.Children.Add(nameLabel);

                Grid scrimGrid = new Grid { BackgroundColor = Color.White };
                scrimGrid.SetValue(Grid.ColumnSpanProperty, 3);
                parentLayout.Children.Add(scrimGrid);

                outerScrimContainer.Children.Add(parentLayout);
                outerScrimContainer.Children.Add(scrimGrid);

                //thumbnail.SetBinding(Image.SourceProperty, nameof(BasemapGalleryItem.ThumbnailImageSource));
                nameLabel.SetBinding(Label.TextProperty, nameof(BasemapGalleryItem.Name));
                scrimGrid.SetBinding(OpacityProperty, nameof(BasemapGalleryItem.IsValid), mode: BindingMode.OneWay, converter: OpacityConverter);

                outerScrimContainer.GestureRecognizers.Add(tapGestureRecognizer);

                return outerScrimContainer;
            });

            DefaultListDataTemplate = new DataTemplate(() =>
            {
                Grid parentLayout = new Grid() { };
                parentLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
                parentLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
                parentLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                Grid imageContainer = new Grid();
                Image fallback = new Image { WidthRequest = 32, HeightRequest = 32, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                fallback.Source = ImageSource.FromResource("Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.Assets.BasemapLight.png", typeof(BasemapGallery).Assembly);
                Image thumbnail = new Image { WidthRequest = 64, HeightRequest = 64, Aspect = Aspect.AspectFill };
                Label nameLabel = new Label { FontSize = 11, TextColor = Color.FromHex("#6e6e6e"), VerticalOptions = LayoutOptions.Center, VerticalTextAlignment = TextAlignment.Center };
                imageContainer.Children.Add(fallback);
                imageContainer.Children.Add(thumbnail);

                parentLayout.Children.Add(imageContainer);
                parentLayout.Children.Add(nameLabel);

                Grid scrimGrid = new Grid { BackgroundColor = Color.White };
                scrimGrid.SetValue(Grid.ColumnSpanProperty, 3);
                parentLayout.Children.Add(scrimGrid);

                imageContainer.SetValue(Grid.ColumnProperty, 1);
                nameLabel.SetValue(Grid.ColumnProperty, 2);

                //thumbnail.SetBinding(Image.SourceProperty, nameof(BasemapGalleryItem.ThumbnailImageSource));
                nameLabel.SetBinding(Label.TextProperty, nameof(BasemapGalleryItem.Name));
                scrimGrid.SetBinding(OpacityProperty, nameof(BasemapGalleryItem.IsValid), mode: BindingMode.OneWay, converter: OpacityConverter);

                parentLayout.GestureRecognizers.Add(tapGestureRecognizer);

                return parentLayout;
            });

            string template = @"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
                                    <Grid>
                                        <CollectionView x:Name=""PART_InnerListView"" HorizontalOptions=""FillAndExpand"" VerticalOptions=""FillAndExpand"" SelectionMode=""Single"" />
                                        <ActivityIndicator x:Name=""PART_LoadingScrim"" IsRunning=""True"" HorizontalOptions=""Center"" VerticalOptions=""Center"" />
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

            // This check may be removable once UWP collectionview supports dynamic item sizing: https://gist.github.com/hartez/7d0edd4182dbc7de65cebc6c67f72e14
            if (Device.RuntimePlatform == Device.UWP)
            {
                styleAfterUpdate = BasemapGalleryViewStyle.List;
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
                    ListView.ItemsLayout = LinearItemsLayout.Vertical;
                }
                else
                {
                    ListView.ItemTemplate = GridItemTemplate;
                    ListView.ItemsLayout = new GridItemsLayout(gridSpanAfterUpdate, ItemsLayoutOrientation.Vertical);
                }

                _currentSelectedSpan = gridSpanAfterUpdate;
                _currentlyAppliedViewStyle = styleAfterUpdate;
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
}