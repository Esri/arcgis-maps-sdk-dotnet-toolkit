using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;
using System.Windows.Shell;
using static System.Net.Mime.MediaTypeNames;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    public class FieldsPopupElementView : Control
    {
        public FieldsPopupElementView()
        {
            DefaultStyleKey = typeof(FieldsPopupElementView);
        }

        public FieldsPopupElement? Element
        {
            get { return GetValue(ElementProperty) as FieldsPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldsPopupElement), typeof(FieldsPopupElementView), new PropertyMetadata(null, (s, e) => ((FieldsPopupElementView)s).RefreshTable()));

        public GeoElement? GeoElement
        {
            get { return GetValue(GeoElementProperty) as GeoElement; }
            set { SetValue(GeoElementProperty, value); }
        }

        public static readonly DependencyProperty GeoElementProperty =
            DependencyProperty.Register(nameof(GeoElement), typeof(GeoElement), typeof(FieldsPopupElementView), new PropertyMetadata(null, (s, e) => ((FieldsPopupElementView)s).RefreshTable()));

        private void RefreshTable()
        {
            var presenter = GetTemplateChild("TableAreaContent") as ContentPresenter;
            if (presenter is null) return;
            if(Element is null || GeoElement is null)
            {
                presenter.Content = null;
                return;
            }
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Pixel) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            int i = 0;
            foreach (var field in Element.Fields)
            {
                g.RowDefinitions.Add(new RowDefinition());
                Border b = new Border() { Background = i % 2 == 1 ? RowEvenBackground : RowOddBackground };
                Grid.SetColumnSpan(b, 3);
                Grid.SetRow(b, i);
                g.Children.Add(b);
                TextBlock t = new TextBlock()
                {
                    Text = field.Label ?? field.FieldName,
                    Style = FieldTextStyle
                };
                Grid.SetRow(t, i);
                g.Children.Add(t);

                Uri? uri = null;
                string? value = GeoElement?.Attributes[field.FieldName]?.ToString();
                bool isUrl = (value != null &&
                    (value.StartsWith("http://") || value.StartsWith("https://"))
                    && Uri.TryCreate(value, UriKind.Absolute, out uri));

                t = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Style = FieldTextStyle
                };

                if (isUrl)
                {
                    Hyperlink hl = new Hyperlink() { NavigateUri = uri };
                    hl.Click += (s, e) =>
                    {
#if NET6_0_OR_GREATER
                        _ = Windows.System.Launcher.LaunchUriAsync(uri);
#else
//TODO
#endif
                    };
                    hl.Inlines.Add("View");
                    t.Inlines.Add(hl);
                }
                else
                {
                    t.Text = value;
                }

                Grid.SetRow(t, i);
                Grid.SetColumn(t, 3);
                g.Children.Add(t);
                i++;
            }
            Border verticalDivider = new Border() { Background = DividerBrush };
            Grid.SetRowSpan(verticalDivider, i);
            Grid.SetColumn(verticalDivider, 1);
            g.Children.Add(verticalDivider);
            presenter.Content = g;
        }

        public Brush RowOddBackground
        {
            get { return (Brush)GetValue(RowOddBackgroundProperty); }
            set { SetValue(RowOddBackgroundProperty, value); }
        }

        public static readonly DependencyProperty RowOddBackgroundProperty =
            DependencyProperty.Register(nameof(RowOddBackground), typeof(Brush), typeof(FieldsPopupElementView), new PropertyMetadata(null));

        public Brush RowEvenBackground
        {
            get { return (Brush)GetValue(RowEvenBackgroundProperty); }
            set { SetValue(RowEvenBackgroundProperty, value); }
        }

        public static readonly DependencyProperty RowEvenBackgroundProperty =
            DependencyProperty.Register(nameof(RowEvenBackground), typeof(Brush), typeof(FieldsPopupElementView), new PropertyMetadata(null));

        public Brush DividerBrush
        {
            get { return (Brush)GetValue(DividerBrushProperty); }
            set { SetValue(DividerBrushProperty, value); }
        }

        public static readonly DependencyProperty DividerBrushProperty =
            DependencyProperty.Register(nameof(DividerBrush), typeof(Brush), typeof(FieldsPopupElementView), new PropertyMetadata(null));

        public Style FieldTextStyle
        {
            get { return (Style)GetValue(FieldTextStyleProperty); }
            set { SetValue(FieldTextStyleProperty, value); }
        }

        public static readonly DependencyProperty FieldTextStyleProperty =
            DependencyProperty.Register(nameof(FieldTextStyle), typeof(Style), typeof(FieldsPopupElementView), new PropertyMetadata(null));


    }
}