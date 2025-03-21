#if MAUI && WINDOWS
using System;
using System.Collections.Generic;
using System.Text;
using Esri.ArcGISRuntime.Mapping.Popups;
using Microsoft.Maui.Controls.Internals;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
{
    /// <summary>
    /// Replaces the Windows CarouselView used by <see cref="MediaPopupElementView"/> due to various CarouselView layout issues on Windows.
    /// </summary>
    internal class CarouselView2 : TemplatedView
    {
        private static readonly ControlTemplate DefaultControlTemplate;

        static CarouselView2()
        {
            DefaultControlTemplate = new ControlTemplate(BuildDefaultTemplate);
        }

        private static object BuildDefaultTemplate()
        {
            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            ContentPresenter content = new ContentPresenter() { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
            Grid.SetColumn(content, 1);
            root.Children.Add(content);
            Button previous = new Button() { WidthRequest = 20, Padding = new Thickness(0), HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Fill, BorderWidth = 0, Text = "", FontFamily = "Segoe MDL2 Assets", BackgroundColor = Colors.Transparent };
            previous.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
            root.Children.Add(previous);
            Button next = new Button() { WidthRequest = 20, Padding = new Thickness(0), HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Fill, BorderWidth = 0, Text = "", FontFamily = "Segoe MDL2 Assets", BackgroundColor = Colors.Transparent };
            next.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
            Grid.SetColumn(next, 2);
            root.Children.Add(next);
            INameScope nameScope = new NameScope();
            NameScope.SetNameScope(root, nameScope);
            nameScope.RegisterName("Content", content);
            nameScope.RegisterName("PreviousButton", previous);
            nameScope.RegisterName("NextButton", next);
            return root;
        }

        public CarouselView2()
        {
            ControlTemplate = DefaultControlTemplate;
            SwipeGestureRecognizer recognizer = new SwipeGestureRecognizer() { Direction = SwipeDirection.Left | SwipeDirection.Right };
            recognizer.Swiped += Recognizer_Swiped;
            GestureRecognizers.Add(recognizer);
        }

        protected override void OnApplyTemplate()
        {
            OnItemTemplatePropertyChanged();
            if (GetTemplateChild("PreviousButton") is Button previousButton)
            {
                previousButton.Clicked += (s, e) => Previous();
            }
            if (GetTemplateChild("NextButton") is Button nextButton)
            {
                nextButton.Clicked += (s, e) => Next();
            }
            base.OnApplyTemplate();
        }

        private void Next()
        {
            if (Position >= (ItemsSource?.Count - 1 ?? 0))
                Position = 0;
            else
                Position++;
        }

        private void Previous()
        {
            if (Position > 0)
                Position--;
            else
                Position = ItemsSource?.Count - 1 ?? 0;
        }

        private void Recognizer_Swiped(object? sender, SwipedEventArgs e)
        {
            if (e.Direction == SwipeDirection.Left)
                Next();
            else if (e.Direction == SwipeDirection.Right)
                Previous();
        }

        public IndicatorView IndicatorView
        {
            set
            {
                LinkToIndicatorView(this, value);
            }
        }

        private static void LinkToIndicatorView(CarouselView2 carouselView, IndicatorView indicatorView)
        {
            if (indicatorView == null)
                return;

            indicatorView.SetBinding(IndicatorView.PositionProperty, static (CarouselView2 cv) => cv.Position, source: carouselView);
            indicatorView.SetBinding(IndicatorView.ItemsSourceProperty, static (CarouselView2 cv) => cv.ItemsSource, source: carouselView);
        }

        private void UpdateBindingContext()
        {
            if (GetTemplateChild("Content") is ContentPresenter content && content.Content is not null)
            {
                content.Content.BindingContext = (ItemsSource != null && Position < ItemsSource.Count) ? ItemsSource[Position] : null;
            }
        }

        private void OnItemTemplatePropertyChanged()
        {
            if (GetTemplateChild("Content") is ContentPresenter contentPresenter)
            {
                contentPresenter.Content = ItemTemplate?.CreateContent() as View;
                UpdateBindingContext();
            }
        }

        public int Position
        {
            get { return (int)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly BindableProperty PositionProperty =
            BindableProperty.Create(nameof(Position), typeof(int), typeof(CarouselView2), 0, propertyChanged: (s, o, n) => ((CarouselView2)s).UpdateBindingContext());

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(CarouselView2), propertyChanged: (s, o, n) => ((CarouselView2)s).OnItemTemplatePropertyChanged());

        public IList<PopupMedia> ItemsSource
        {
            get { return (IList<PopupMedia>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IList<PopupMedia>), typeof(CarouselView2), propertyChanged: (s, o, n) => ((CarouselView2)s).OnItemsSourcePropertyChanged());

        private void OnItemsSourcePropertyChanged()
        {
            if (GetTemplateChild("PreviousButton") is Button previousButton)
                previousButton.IsVisible = (ItemsSource?.Count ?? 0) > 1;
            if (GetTemplateChild("NextButton") is Button nextButton)
                nextButton.IsVisible = (ItemsSource?.Count ?? 0) > 1;
            UpdateBindingContext();
        }
    }
}
#endif