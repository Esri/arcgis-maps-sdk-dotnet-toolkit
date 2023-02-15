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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using System.Windows.Documents;

namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Supporting control for the <see cref="Esri.ArcGISRuntime.Toolkit.UI.Controls.PopupViewer"/> control,
    /// used for rendering a <see cref="FieldsPopupElement"/>.
    /// </summary>
    public class FieldsPopupElementView : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldsPopupElementView"/> class.
        /// </summary>
        public FieldsPopupElementView()
        {
            DefaultStyleKey = typeof(FieldsPopupElementView);
        }

        /// <summary>
        /// Gets or sets the FieldsPopupElement.
        /// </summary>
        public FieldsPopupElement? Element
        {
            get { return GetValue(ElementProperty) as FieldsPopupElement; }
            set { SetValue(ElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(FieldsPopupElement), typeof(FieldsPopupElementView), new PropertyMetadata(null, (s, e) => ((FieldsPopupElementView)s).RefreshTable()));

        /// <summary>
        /// Gets or sets the GeoElement who's attribute will be showed with the <see cref="FieldsPopupElement"/>.
        /// </summary>
        public GeoElement? GeoElement
        {
            get { return GetValue(GeoElementProperty) as GeoElement; }
            set { SetValue(GeoElementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoElement"/> dependency property.
        /// </summary>
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
                string? value = GeoElement?.Attributes.ContainsKey(field.FieldName) == true ? GeoElement.Attributes[field.FieldName]?.ToString() : string.Empty;
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

        /// <summary>
        /// Gets or sets the background of the odd rows in the table.
        /// </summary>
        public Brush RowOddBackground
        {
            get { return (Brush)GetValue(RowOddBackgroundProperty); }
            set { SetValue(RowOddBackgroundProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RowOddBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowOddBackgroundProperty =
            DependencyProperty.Register(nameof(RowOddBackground), typeof(Brush), typeof(FieldsPopupElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the background of the even rows in the table.
        /// </summary>
        public Brush RowEvenBackground
        {
            get { return (Brush)GetValue(RowEvenBackgroundProperty); }
            set { SetValue(RowEvenBackgroundProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RowEvenBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowEvenBackgroundProperty =
            DependencyProperty.Register(nameof(RowEvenBackground), typeof(Brush), typeof(FieldsPopupElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the vertical divider brush in the table.
        /// </summary>
        public Brush DividerBrush
        {
            get { return (Brush)GetValue(DividerBrushProperty); }
            set { SetValue(DividerBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DividerBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DividerBrushProperty =
            DependencyProperty.Register(nameof(DividerBrush), typeof(Brush), typeof(FieldsPopupElementView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="TextBlock"/> style applied to the text in the table.
        /// </summary>
        public Style FieldTextStyle
        {
            get { return (Style)GetValue(FieldTextStyleProperty); }
            set { SetValue(FieldTextStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FieldTextStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FieldTextStyleProperty =
            DependencyProperty.Register(nameof(FieldTextStyle), typeof(Style), typeof(FieldsPopupElementView), new PropertyMetadata(null));


    }
}