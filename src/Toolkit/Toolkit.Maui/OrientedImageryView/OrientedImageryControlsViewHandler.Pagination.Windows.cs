#if WINDOWS
using System.Globalization;
using Esri.ArcGISRuntime.Toolkit.Maui.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
using NativeGrid = Microsoft.UI.Xaml.Controls.Grid;
using NativeThickness = Microsoft.UI.Xaml.Thickness;
using NativeVisibility = Microsoft.UI.Xaml.Visibility;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

internal sealed partial class OrientedImageryControlsViewHandler
{
    #region Image pagination

    private PaginationState? _paginationState;

    private readonly record struct PaginationState(bool Sequential, int Selected, int Start, int Count, int Total,
        bool Previous, bool Next, bool Searching, bool PreviousTabStop, bool NextTabStop);

    private void RefreshPagination(OrientedImageryView owner)
    {
        var model = owner.ViewModel;
        int selected = -1;
        for (int i = 0; i < model.Images.Count; i++)
            if (model.Images[i] == model.SelectedImage) selected = i;
        double width = VirtualView.Width > 0 ? VirtualView.Width : owner.Width;
        var (start, count) = OrientedImageryPagination.GetRange(model.Images.Count, selected, Math.Max(35, width - 66), 35);
        var state = new PaginationState(model.IsSequentialNavigationEnabled, selected, start, count, model.Images.Count,
            model.SelectPreviousImageCommand.CanExecute(null), model.SelectNextImageCommand.CanExecute(null), owner.DesktopIsSearching,
            !owner.HasImageOverlayArrow(false), !owner.HasImageOverlayArrow(true));
        if (_paginationState == state && _model == model) return;
        // Keep keyboard focus on the same action or image when the page range changes.
        var focused = PlatformView.XamlRoot == null ? null :
            FocusManager.GetFocusedElement(PlatformView.XamlRoot) as NativeButton;
        bool restoreFocus = focused != null &&
            PlatformView.Children.OfType<PaginationRow>().Any(panel => panel.Children.Contains(focused));
        string? focusedId = restoreFocus ? AutomationProperties.GetAutomationId(focused!) : null;
        var focusState = restoreFocus ? focused!.FocusState : FocusState.Unfocused;
        PaginationRow row;
        if (_model == model && _paginationState is { } previous &&
            previous.Sequential == state.Sequential && (state.Sequential || previous.Count == state.Count) &&
            PlatformView.Children.FirstOrDefault() is PaginationRow existingRow)
        {
            row = existingRow;
        }
        else
        {
            Clear();
            _model = model;
            row = CreatePaginationRow(owner, state);
            PlatformView.Children.Add(row);
        }
        _paginationState = state;
        UpdatePaginationRow(row, state);
        ((IView)VirtualView).InvalidateMeasure();
        if (restoreFocus)
            RestorePaginationFocus(row, focusedId, selected, focusState);
    }

    private PaginationRow CreatePaginationRow(OrientedImageryView owner, PaginationState state)
    {
        var model = owner.ViewModel;
        var row = new PaginationRow();
        NativeButton PageButton(bool next, bool label)
        {
            var button = new NativeButton
            {
                Content = label ? Text(next ? "NextLabel" : "PreviousLabel") : new FontIcon
                {
                    Glyph = next ? ToolkitIcons.ChevronRight : ToolkitIcons.ChevronLeft,
                    FontFamily = IconFont, FontSize = 16,
                },
                MinHeight = 32, MinWidth = 0, Padding = new NativeThickness(label ? 4 : 0),
                BorderThickness = new NativeThickness(0), CornerRadius = new CornerRadius(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                AllowFocusWhenDisabled = true,
                IsTabStop = false,
            };
            if (!label) button.Width = 33;
            string id = next ? "OrientedImageryPaginatorNext" : "OrientedImageryPaginatorPrevious";
            Describe(button, owner.ImagePageActionKey(next), label ? id + "Label" : id);
            button.Click += (_, _) =>
            {
                var command = next ? model.SelectNextImageCommand : model.SelectPreviousImageCommand;
                if (!owner.DesktopIsSearching && command.CanExecute(null)) command.Execute(null);
            };
            return button;
        }
        row.Children.Add(PageButton(false, false));
        if (state.Sequential)
        {
            row.Children.Add(PageButton(false, true));
            row.Children.Add(PageIndicator(".", true));
        }
        else
        {
            for (int slot = 0; slot < state.Count; slot++)
            {
                int pageSlot = slot;
                var button = new NativeButton
                {
                    Content = PageIndicator("", false),
                    MinWidth = 35, MinHeight = 32, Padding = new NativeThickness(0),
                    BorderThickness = new NativeThickness(0), CornerRadius = new CornerRadius(0),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                };
                button.Click += (_, _) =>
                {
                    int index = (_paginationState?.Start ?? 0) + pageSlot;
                    if (VirtualView.Owner?.ViewModel == model && !model.IsSequentialNavigationEnabled && index < model.Images.Count)
                        model.SelectedImage = model.Images[index];
                };
                button.PreviewKeyDown += (_, e) =>
                {
                    if (e.Key is not (Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Right))
                        return;
                    if (VirtualView.Owner == owner && owner.ViewModel == model &&
                        !owner.DesktopIsSearching && !model.IsSequentialNavigationEnabled)
                    {
                        var command = e.Key == Windows.System.VirtualKey.Right
                            ? model.SelectNextImageCommand : model.SelectPreviousImageCommand;
                        if (command.CanExecute(null)) command.Execute(null);
                    }
                    e.Handled = true;
                };
                row.Children.Add(button);
            }
        }
        if (state.Sequential) row.Children.Add(PageButton(true, true));
        row.Children.Add(PageButton(true, false));
        return row;
    }

    private static void RestorePaginationFocus(PaginationRow row, string? focusedId, int selected, FocusState focusState)
    {
        // Numbered pagination follows selection, including when the visible range shifts.
        if (focusedId?.StartsWith("OrientedImageryPage", StringComparison.Ordinal) == true)
            focusedId = "OrientedImageryPage" + selected.ToString(CultureInfo.InvariantCulture);
        var target = row.Children.OfType<NativeButton>().FirstOrDefault(button => AutomationProperties.GetAutomationId(button) == focusedId);
        target ??= row.Children.OfType<NativeButton>().FirstOrDefault(button => AutomationProperties.GetAutomationId(button) == "OrientedImageryPage" + selected.ToString(CultureInfo.InvariantCulture));
        target?.Focus(focusState);
    }

    private static void UpdatePaginationRow(PaginationRow row, PaginationState state)
    {
        var previous = (NativeButton)row.Children[0];
        previous.IsEnabled = !state.Searching && state.Previous;
        previous.IsTabStop = state.PreviousTabStop;
        var next = (NativeButton)row.Children[row.Children.Count - 1];
        next.IsEnabled = !state.Searching && state.Next;
        next.IsTabStop = state.NextTabStop;
        if (state.Sequential)
        {
            ((NativeButton)row.Children[1]).IsEnabled = previous.IsEnabled;
            ((NativeButton)row.Children[3]).IsEnabled = next.IsEnabled;
            return;
        }
        for (int slot = 0; slot < state.Count; slot++)
        {
            int index = state.Start + slot;
            var button = (NativeButton)row.Children[slot + 1];
            var indicator = (NativeGrid)button.Content;
            ((TextBlock)indicator.Children[0]).Text = (index + 1).ToString(CultureInfo.CurrentCulture);
            ((Microsoft.UI.Xaml.Controls.Border)indicator.Children[1]).Visibility =
                index == state.Selected ? NativeVisibility.Visible : NativeVisibility.Collapsed;
            button.IsEnabled = !state.Searching;
            button.IsTabStop = slot == Math.Clamp(state.Selected - state.Start, 0, state.Count - 1);
            AutomationProperties.SetName(button, string.Format(CultureInfo.CurrentCulture,
                Text(index == state.Selected ? "SelectedPageFormat" : "GoToPageFormat"), index + 1, state.Total));
            AutomationProperties.SetAutomationId(button, "OrientedImageryPage" + index.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static NativeGrid PageIndicator(string text, bool selected)
    {
        var grid = new NativeGrid { MinWidth = 35, MinHeight = 32 };
        grid.Children.Add(new TextBlock
        {
            Text = text, FontSize = 14, Margin = new NativeThickness(4, 2, 4, 4),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
        });
        var underline = ThemeBorder("SystemControlHighlightAccentBrush");
        underline.Height = 2;
        underline.Margin = new NativeThickness(1, 0, 1, 0);
        underline.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Bottom;
        underline.Visibility = selected ? NativeVisibility.Visible : NativeVisibility.Collapsed;
        grid.Children.Add(underline);
        return grid;
    }

    #endregion

    #region Pagination row layout

    private sealed class PaginationRow : Panel
    {
        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            double width = 0, height = 0, rowWidth = 0, rowHeight = 0;
            foreach (var child in Children)
            {
                child.Measure(new Windows.Foundation.Size(availableSize.Width, double.PositiveInfinity));
                var size = child.DesiredSize;
                if (rowWidth > 0 && rowWidth + size.Width > availableSize.Width)
                {
                    width = Math.Max(width, rowWidth);
                    height += rowHeight;
                    rowWidth = rowHeight = 0;
                }
                rowWidth += size.Width;
                rowHeight = Math.Max(rowHeight, size.Height);
            }
            return new Windows.Foundation.Size(Math.Max(width, rowWidth), height + rowHeight);
        }

        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            double y = 0, width = 0, height = 0;
            var row = new List<UIElement>();
            void ArrangeRow()
            {
                double x = Math.Max(0, (finalSize.Width - width) / 2);
                foreach (var child in row)
                {
                    child.Arrange(new Windows.Foundation.Rect(x, y, child.DesiredSize.Width, height));
                    x += child.DesiredSize.Width;
                }
                y += height;
                row.Clear();
                width = height = 0;
            }
            foreach (var child in Children)
            {
                if (width > 0 && width + child.DesiredSize.Width > finalSize.Width)
                    ArrangeRow();
                row.Add(child);
                width += child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }
            ArrangeRow();
            return finalSize;
        }
    }

    #endregion
}
#endif
