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

using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
#if MAUI
using Esri.ArcGISRuntime.Toolkit.Maui;
#else
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
#endif

#if MAUI && WINDOWS
using TextBlock = Esri.ArcGISRuntime.Toolkit.Maui.Primitives.SelectableLabel;
using ChildElement = Microsoft.Maui.Controls.View;
#elif MAUI // iOS, Android, generic
using TextBlock = Microsoft.Maui.Controls.Label;
using ChildElement = Microsoft.Maui.Controls.View;
#elif WPF
using System.Windows.Automation;
using ChildElement = System.Windows.FrameworkElement;
#elif WINUI
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Documents;
using ChildElement = Microsoft.UI.Xaml.FrameworkElement;
#elif WINDOWS_UWP
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Documents;
using ChildElement = Windows.UI.Xaml.FrameworkElement;
#endif

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    public partial class FieldsPopupElementView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldsPopupElementView"/> class.
        /// </summary>
        public FieldsPopupElementView()
        {
#if MAUI
            RowEvenBackground = new SolidColorBrush(Color.FromRgb(0xFB, 0xFB, 0xFB));
            RowOddBackground = new SolidColorBrush(Color.FromRgb(0xED, 0xED, 0xED));
            DividerBrush = new SolidColorBrush(Color.FromRgba(0x33, 0x33, 0x33, 0x11));
            ControlTemplate = DefaultControlTemplate;
            FieldTextStyle = DefaultFieldTextStyle;
#else
            DefaultStyleKey = typeof(FieldsPopupElementView);
#endif
        }

        /// <inheritdoc />
#if WINDOWS_XAML || MAUI
        protected override void OnApplyTemplate()
#elif WPF
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
            RefreshTable();
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
            PropertyHelper.CreateProperty<FieldsPopupElement, FieldsPopupElementView>(nameof(Element), null, (s, oldValue, newValue) => s.RefreshTable());

        private void RefreshTable()
        {
            var presenter = GetTemplateChild(TableAreaContentName) as ContentPresenter;
            if (presenter is null) return;
            if (Element is null || !Element.Fields.Any())
            {
                presenter.Content = null;
                return;
            }
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
#if MAUI
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Absolute) });
#else
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Pixel) });
#endif
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            int i = 0;
            var rowCount = Math.Min(Element.Labels.Count, Element.FormattedValues.Count);
            for (i = 0; i < rowCount; i++)
            {

                g.RowDefinitions.Add(new RowDefinition());
                Border b = new Border() { Background = i % 2 == 1 ? RowEvenBackground : RowOddBackground };
#if MAUI
                b.StrokeThickness = 0;
                b.Stroke = null;
#endif
                Grid.SetColumnSpan(b, 3);
                Grid.SetRow(b, i);
                g.Children.Add(b);

                var label = CreateTextCell(Element.Labels[i], wrap: false);

                Grid.SetRow(label, i);
                g.Children.Add(label);

                ChildElement valueCell;
                var strValue = Element.FormattedValues[i];
                Uri? uri = null;
                bool isUrl = (strValue != null &&
                   (strValue.StartsWith("http://") || strValue.StartsWith("https://"))
                   && Uri.TryCreate(strValue, UriKind.Absolute, out uri));
                if (isUrl)
                {
                    valueCell = CreateHyperlinkCell(uri!);
                }
                else
                {
                    valueCell = CreateTextCell(strValue, wrap: true);
                }

                Grid.SetRow(valueCell, i);
                Grid.SetColumn(valueCell, 2);
                g.Children.Add(valueCell);
                AutomationProperties.SetLabeledBy(valueCell, label);
            }

            Border verticalDivider = new Border()
            {
                Background = DividerBrush,
#if MAUI
                StrokeThickness = 0
#endif
            };
            Grid.SetRowSpan(verticalDivider, i);
            Grid.SetColumn(verticalDivider, 1);
            g.Children.Add(verticalDivider);
            presenter.Content = g;
        }

#if !WPF // see FieldsPopupElementView.Windows.cs for WPF implementation
        private ChildElement CreateTextCell(string? text, bool wrap)
        {
            var t = new TextBlock
            {
                Text = text ?? "",
                Style = FieldTextStyle,
            };
            if (wrap)
            {
#if MAUI
                t.LineBreakMode = LineBreakMode.WordWrap;
#else
                t.TextWrapping = TextWrapping.Wrap;
#endif
            }
            return t;
        }

        private ChildElement CreateHyperlinkCell(Uri uri)
        {
#if MAUI
            // Don't use the SelectableLabel here since hyperlinks don't need to be selectable and it can cause issues with the tap gesture recognizer.
            var t = new Microsoft.Maui.Controls.Label
#else
            var t = new TextBlock
#endif
            {
                Style = FieldTextStyle,
#if MAUI
                LineBreakMode = LineBreakMode.WordWrap,
#else
                TextWrapping = TextWrapping.Wrap,
#endif
            };
#if MAUI
            var hl = new Span() { Text = Properties.Resources.GetString("PopupViewerViewHyperlinkText"), TextDecorations = TextDecorations.Underline };
            var gestureRecognizer = new TapGestureRecognizer() { NumberOfTapsRequired = 1 };
            gestureRecognizer.Tapped += (s, e) =>
            {
                if (uri is not null)
                    PopupViewer.GetPopupViewerParent(this)?.OnHyperlinkClicked(uri);
            };
            t.GestureRecognizers.Add(gestureRecognizer);
            t.FormattedText = new FormattedString();
            t.FormattedText.Spans.Add(hl);
#else
            Hyperlink hl = new Hyperlink() { NavigateUri = uri };
            hl.Click += (s, e) =>
            {
                if (uri is not null)
                    PopupViewer.GetPopupViewerParent(this)?.OnHyperlinkClicked(uri);
            };
#if WINDOWS_XAML
            hl.Inlines.Add(new Run() { Text = Properties.Resources.GetString("PopupViewerViewHyperlinkText") });
#else
            hl.Inlines.Add(Properties.Resources.GetString("PopupViewerViewHyperlinkText"));
#endif
            t.Inlines.Add(hl);
#endif
            return t;
        }
#endif

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
            PropertyHelper.CreateProperty<Brush, FieldsPopupElementView>(nameof(RowOddBackground));

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
            PropertyHelper.CreateProperty<Brush, FieldsPopupElementView>(nameof(RowEvenBackground));

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
            PropertyHelper.CreateProperty<Brush, FieldsPopupElementView>(nameof(DividerBrush));

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
            PropertyHelper.CreateProperty<Style, FieldsPopupElementView>(nameof(FieldTextStyle));
    }
}