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

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class SearchView : TemplatedView, INotifyPropertyChanged
{
#pragma warning disable SA1306, SA1310, SX1309 // Field names should begin with lower-case letter
    private Entry? PART_Entry;
    private ImageButton? PART_CancelButton;
    private ImageButton? PART_SearchButton;
    private ImageButton? PART_SourceSelectButton;
    private Label? PART_ResultLabel;
    private CollectionView? PART_SuggestionsView;
    private CollectionView? PART_ResultView;
    private CollectionView? PART_SourcesView;
    private Button? PART_RepeatButton;
    private Grid? PART_ResultContainer;
    private Grid? PART_RepeatButtonContainer;
#pragma warning restore SA1306, SA1310, SX1309 // Field names should begin with lower-case letter
    private const string FOREGROUND_LIGHT = "#151515";
    private const string FOREGROUND_DARK = "#FFF";
    private const string BACKGROUND_LIGHT = "#FFF";
    private const string BACKGROUND_DARK = "#2B2B2B";

    private static readonly DataTemplate DefaultResultTemplate;
    private static readonly DataTemplate DefaultSuggestionTemplate;
    private static readonly DataTemplate DefaultSuggestionGroupHeaderTemplate;
    private static readonly ControlTemplate DefaultControlTemplate;
    private static readonly ByteArrayToImageSourceConverter ImageSourceConverter;
    private static readonly BoolToCollectionIconImageConverter CollectionIconConverter;
    private static readonly EmptyStringToBoolConverter EmptyStringConverter;

    static SearchView()
    {
        ImageSourceConverter = new ByteArrayToImageSourceConverter();
        CollectionIconConverter = new BoolToCollectionIconImageConverter();
        EmptyStringConverter = new EmptyStringToBoolConverter();
        DefaultSuggestionGroupHeaderTemplate = new DataTemplate(() =>
        {
            Grid containingGrid = new Grid();
            containingGrid.SetAppThemeColor(Grid.BackgroundColorProperty, Color.FromArgb("#4e4e4e"), Color.FromArgb("#151515"));

            Label textLabel = new Label();
            textLabel.SetBinding(Label.TextProperty, "Key.DisplayName");
            textLabel.Margin = new Thickness(4);
            textLabel.TextColor = Colors.White;
            textLabel.FontSize = 14;
            textLabel.VerticalTextAlignment = TextAlignment.Center;
            containingGrid.Children.Add(textLabel);
            return containingGrid;
        });
        DefaultSuggestionTemplate = new DataTemplate(() =>
        {
            Grid containingGrid = new Grid();
            containingGrid.SetAppThemeColor(Grid.BackgroundColorProperty, Color.FromArgb(BACKGROUND_LIGHT), Color.FromArgb(BACKGROUND_DARK));
            containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            containingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid textStack = new Grid();
            textStack.BackgroundColor = Colors.Transparent;
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
            titleLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb(FOREGROUND_LIGHT), Color.FromArgb(FOREGROUND_DARK));

            Label subtitleLabel = new Label();
            subtitleLabel.SetBinding(Label.TextProperty, nameof(SearchSuggestion.DisplaySubtitle));
            subtitleLabel.SetBinding(Label.IsVisibleProperty, nameof(SearchSuggestion.DisplaySubtitle), converter: EmptyStringConverter);
            subtitleLabel.VerticalOptions = LayoutOptions.Start;
            subtitleLabel.VerticalTextAlignment = TextAlignment.Start;
            subtitleLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb(FOREGROUND_LIGHT), Color.FromArgb(FOREGROUND_DARK));

            textStack.Children.Add(titleLabel);
            textStack.Children.Add(subtitleLabel);
            Grid.SetRow(titleLabel, 0);
            Grid.SetRow(subtitleLabel, 1);

            containingGrid.Children.Add(imageView);
            containingGrid.Children.Add(textStack);

            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(imageView, 0);

            return containingGrid;
        });
        DefaultResultTemplate = new DataTemplate(() =>
        {
            Grid containingGrid = new Grid();
            containingGrid.Padding = new Thickness(2, 4, 2, 4);
            containingGrid.SetAppThemeColor(Grid.BackgroundColorProperty, Color.FromArgb(BACKGROUND_LIGHT), Color.FromArgb(BACKGROUND_DARK));

            containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            containingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            containingGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid textStack = new Grid();
            textStack.BackgroundColor = Colors.Transparent;
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
            titleLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb(FOREGROUND_LIGHT), Color.FromArgb(FOREGROUND_DARK));

            Label subtitleLabel = new Label();
            subtitleLabel.SetBinding(Label.TextProperty, nameof(SearchResult.DisplaySubtitle));
            subtitleLabel.SetBinding(Label.IsVisibleProperty, nameof(SearchResult.DisplaySubtitle), converter: EmptyStringConverter);
            subtitleLabel.VerticalOptions = LayoutOptions.Start;
            subtitleLabel.VerticalTextAlignment = TextAlignment.Start;
            subtitleLabel.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb(FOREGROUND_LIGHT), Color.FromArgb(FOREGROUND_DARK));

            textStack.Children.Add(titleLabel);
            textStack.Children.Add(subtitleLabel);
            Grid.SetRow(titleLabel, 0);
            Grid.SetRow(subtitleLabel, 1);

            containingGrid.Children.Add(imageView);
            containingGrid.Children.Add(textStack);

            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(imageView, 0);

            return containingGrid;
        });

        string template =
$@"<ControlTemplate xmlns=""http://schemas.microsoft.com/dotnet/2021/maui"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" 
xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Maui"">
<Grid RowSpacing=""0"" ColumnSpacing=""0"">
<Grid.Resources>
        <Style TargetType=""Grid"">
            <Setter Property=""Background"" Value=""{{AppThemeBinding Dark={BACKGROUND_DARK},Light={BACKGROUND_LIGHT}}}"" />
        </Style>
        <Style TargetType=""CollectionView"">
            <Setter Property=""Background"" Value=""{{AppThemeBinding Dark={BACKGROUND_DARK},Light={BACKGROUND_LIGHT}}}"" />
        </Style>
        <Style TargetType=""Entry"">
            <Setter Property=""Background"" Value=""{{AppThemeBinding Dark={BACKGROUND_DARK},Light={BACKGROUND_LIGHT}}}"" />
        </Style>

</Grid.Resources>
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
    <Grid Grid.Row=""0"" Grid.ColumnSpan=""3""/>
    <ImageButton x:Name=""{nameof(PART_SourceSelectButton)}"" Grid.Column=""0"" WidthRequest=""32"" HeightRequest=""32"" Padding=""4"" BackgroundColor=""Transparent"" Margin=""0"" />
    <Entry x:Name=""{nameof(PART_Entry)}"" Grid.Column=""1"" Grid.Row=""0"" TextColor=""{{AppThemeBinding Light={FOREGROUND_LIGHT}, Dark={FOREGROUND_DARK}}}"" />
    <ImageButton x:Name=""{nameof(PART_CancelButton)}"" Grid.Column=""1"" HorizontalOptions=""End"" WidthRequest=""32"" HeightRequest=""32"" Padding=""4"" BackgroundColor=""Transparent"" />
    <ImageButton x:Name=""{nameof(PART_SearchButton)}"" Grid.Column=""2"" WidthRequest=""32"" HeightRequest=""32"" Padding=""4"" BackgroundColor=""Transparent"" />
    <CollectionView x:Name=""{nameof(PART_SuggestionsView)}"" SelectionMode=""Single"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" Grid.RowSpan=""2""  HeightRequest=""175"" />
    <CollectionView x:Name=""{nameof(PART_ResultView)}"" SelectionMode=""Single"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" Grid.RowSpan=""1"" HeightRequest=""200"" />
    <CollectionView x:Name=""{nameof(PART_SourcesView)}"" SelectionMode=""Single"" Grid.Column=""0"" Grid.ColumnSpan=""3"" Grid.Row=""1"" HeightRequest=""150"" />
    <Grid x:Name=""{nameof(PART_ResultContainer)}"" Grid.ColumnSpan=""3"" Grid.Row=""1"" Padding=""8""><Label x:Name=""{nameof(PART_ResultLabel)}"" HorizontalOptions=""Center"" VerticalOptions=""Center"" FontAttributes=""Bold"" /></Grid>
    <Grid x:Name=""{nameof(PART_RepeatButtonContainer)}"" Grid.Column=""0"" Grid.ColumnSpan=""3""  Grid.Row=""2"">
        <Button x:Name=""{nameof(PART_RepeatButton)}"" BackgroundColor=""{{AppThemeBinding Light=#007AC2, Dark=#00619B}}"" TextColor=""White"" CornerRadius=""0"" />
    </Grid>
</Grid>
</ControlTemplate>";
        DefaultControlTemplate = Microsoft.Maui.Controls.Xaml.Extensions.LoadFromXaml(new ControlTemplate(), template);
    }
}
