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
using System.ComponentModel;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Xamarin.Forms;
using Grid = Xamarin.Forms.Grid;
using XForms = Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    public partial class SearchView : TemplatedView, INotifyPropertyChanged
    {
#pragma warning disable SA1306, SA1310, SX1309 // Field names should begin with lower-case letter
        private Entry? PART_Entry;
        private ImageButton? PART_CancelButton;
        private ImageButton? PART_SearchButton;
        private ImageButton? PART_SourceSelectButton;
        private Label? PART_ResultLabel;
        private ListView? PART_SuggestionsView;
        private ListView? PART_ResultView;
        private ListView? PART_SourcesView;
        private Button? PART_RepeatButton;
        private Grid? PART_ResultContainer;
        private Grid? PART_RepeatButtonContainer;
#pragma warning restore SA1306, SA1310, SX1309 // Field names should begin with lower-case letter

        private static readonly DataTemplate DefaultResultTemplate;
        private static readonly DataTemplate DefaultSuggestionTemplate;
        private static readonly ControlTemplate DefaultControlTemplate;
        private static readonly ByteArrayToImageSourceConverter ImageSourceConverter;
        private static readonly BoolToCollectionIconImageConverter CollectionIconConverter;
        private static readonly EmptyStringToBoolConverter EmptyStringConverter;

        static SearchView()
        {
            ImageSourceConverter = new ByteArrayToImageSourceConverter();
            CollectionIconConverter = new BoolToCollectionIconImageConverter();
            EmptyStringConverter = new EmptyStringToBoolConverter();
            DefaultSuggestionTemplate = new DataTemplate(() =>
            {
                var viewCell = new ViewCell();

                Grid containingGrid = new Grid();

                containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                containingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid textStack = new Grid();
                textStack.VerticalOptions = LayoutOptions.Center;
                textStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                textStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Image imageView = new Image();
                imageView.SetBinding(Image.SourceProperty, nameof(SearchSuggestion.IsCollection), converter: CollectionIconConverter);
                imageView.WidthRequest = 16;
                imageView.HeightRequest = 16;
                imageView.Margin = new Thickness(4);
                imageView.VerticalOptions = LayoutOptions.Center;

                Label titleLabel = new Label();
                titleLabel.SetBinding(Label.TextProperty, nameof(SearchSuggestion.DisplayTitle));
                titleLabel.VerticalOptions = LayoutOptions.End;
                titleLabel.VerticalTextAlignment = TextAlignment.End;

                Label subtitleLabel = new Label();
                subtitleLabel.SetBinding(Label.TextProperty, nameof(SearchSuggestion.DisplaySubtitle));
                subtitleLabel.SetBinding(Label.IsVisibleProperty, nameof(SearchSuggestion.DisplaySubtitle), converter: EmptyStringConverter);
                subtitleLabel.VerticalOptions = LayoutOptions.Start;
                subtitleLabel.VerticalTextAlignment = TextAlignment.Start;

                textStack.Children.Add(titleLabel);
                textStack.Children.Add(subtitleLabel);
                Grid.SetRow(titleLabel, 0);
                Grid.SetRow(subtitleLabel, 1);

                containingGrid.Children.Add(imageView);
                containingGrid.Children.Add(textStack);

                Grid.SetColumn(textStack, 1);
                Grid.SetColumn(imageView, 0);

                viewCell.View = containingGrid;
                return viewCell;
            });
            DefaultResultTemplate = new DataTemplate(() =>
            {
                var viewCell = new ViewCell();

                Grid containingGrid = new Grid();
                containingGrid.Padding = new Thickness(2, 4, 2, 4);

                containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                containingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid textStack = new Grid();
                textStack.VerticalOptions = LayoutOptions.Center;
                textStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                textStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Image imageView = new Image();
                imageView.SetBinding(Image.SourceProperty, nameof(SearchResult.MarkerImageData), converter: ImageSourceConverter);
                imageView.WidthRequest = 24;
                imageView.HeightRequest = 24;
                imageView.Margin = new Thickness(4);
                imageView.VerticalOptions = LayoutOptions.Center;

                Label titleLabel = new Label();
                titleLabel.SetBinding(Label.TextProperty, nameof(SearchResult.DisplayTitle));
                titleLabel.FontAttributes = FontAttributes.Bold;
                titleLabel.VerticalOptions = LayoutOptions.End;
                titleLabel.VerticalTextAlignment = TextAlignment.End;

                Label subtitleLabel = new Label();
                subtitleLabel.SetBinding(Label.TextProperty, nameof(SearchResult.DisplaySubtitle));
                subtitleLabel.SetBinding(Label.IsVisibleProperty, nameof(SearchResult.DisplaySubtitle), converter: EmptyStringConverter);
                subtitleLabel.VerticalOptions = LayoutOptions.Start;
                subtitleLabel.VerticalTextAlignment = TextAlignment.Start;

                textStack.Children.Add(titleLabel);
                textStack.Children.Add(subtitleLabel);
                Grid.SetRow(titleLabel, 0);
                Grid.SetRow(subtitleLabel, 1);

                containingGrid.Children.Add(imageView);
                containingGrid.Children.Add(textStack);

                Grid.SetColumn(textStack, 1);
                Grid.SetColumn(imageView, 0);

                viewCell.View = containingGrid;
                return viewCell;
            });

            string template =
$@"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" 
xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
<Grid RowSpacing=""0"" ColumnSpacing=""0"" >
    <Grid.ColumnDefinitions>
    <ColumnDefinition Width=""Auto"" />
    <ColumnDefinition Width=""*"" />
    <ColumnDefinition Width=""32"" />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
    <RowDefinition Height=""Auto"" />
    <RowDefinition Height=""Auto"" />
    <RowDefinition Height=""Auto"" />
    </Grid.RowDefinitions>
    <Grid Grid.Row=""0"" Grid.ColumnSpan=""3"" BackgroundColor=""White"" />
    <ImageButton x:Name=""{nameof(PART_SourceSelectButton)}"" Grid.Column=""0"" WidthRequest=""32"" HeightRequest=""32"" Padding=""4"" BackgroundColor=""Transparent"" Margin=""0"" />
    <Entry x:Name=""{nameof(PART_Entry)}"" Grid.Column=""1"" Grid.Row=""0"" BackgroundColor=""White"" TextColor=""Black"" />
    <ImageButton x:Name=""{nameof(PART_CancelButton)}"" Grid.Column=""1"" HorizontalOptions=""End"" WidthRequest=""32"" HeightRequest=""32"" Padding=""4"" BackgroundColor=""Transparent"" />
    <ImageButton x:Name=""{nameof(PART_SearchButton)}"" Grid.Column=""2"" WidthRequest=""32"" HeightRequest=""32"" Padding=""4"" BackgroundColor=""Transparent"" />
    <ListView x:Name=""{nameof(PART_SuggestionsView)}"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" Grid.RowSpan=""2"" HasUnevenRows=""true"" BackgroundColor=""White"" HeightRequest=""200"">
        <ListView.GroupHeaderTemplate>
            <DataTemplate><ViewCell><Grid BackgroundColor=""#4e4e4e""><Label Text=""{{Binding Key.DisplayName}}"" Margin=""4"" TextColor=""White"" FontSize=""14"" VerticalTextAlignment=""Center"" /></Grid></ViewCell></DataTemplate>
        </ListView.GroupHeaderTemplate>
    </ListView>
    <ListView x:Name=""{nameof(PART_ResultView)}"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" Grid.RowSpan=""1"" HasUnevenRows=""true"" BackgroundColor=""White"" HeightRequest=""200"" />
    <ListView x:Name=""{nameof(PART_SourcesView)}"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" BackgroundColor=""White"" HeightRequest=""200"" />
    <Grid x:Name=""{nameof(PART_ResultContainer)}"" BackgroundColor=""White"" Grid.ColumnSpan=""3"" Grid.Row=""1"" Padding=""8""><Label x:Name=""{nameof(PART_ResultLabel)}"" HorizontalOptions=""Center"" VerticalOptions=""Center"" FontAttributes=""Bold"" /></Grid>
    <Grid x:Name=""{nameof(PART_RepeatButtonContainer)}"" BackgroundColor=""White"" Grid.Column=""0"" Grid.ColumnSpan=""3""  Grid.Row=""2"">
        <Button x:Name=""{nameof(PART_RepeatButton)}"" BackgroundColor=""#007AC2"" TextColor=""White"" />
    </Grid>
</Grid>
</ControlTemplate>";
            DefaultControlTemplate = XForms.Extensions.LoadFromXaml(new ControlTemplate(), template);
        }
    }
}
