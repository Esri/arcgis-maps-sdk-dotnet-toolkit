using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

/// <summary>
/// Displays an adaptive, centered range of equally sized page buttons.
/// </summary>
internal class PaginatorPresenterPanel : Panel
{
    private Size _pageSize;

    /// <summary>
    /// Gets or sets the index of the currently-selected page.
    /// </summary>
    public int SelectedPageIndex
    {
        get => (int)GetValue(SelectedPageIndexProperty);
        set => SetValue(SelectedPageIndexProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SelectedPageIndex" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedPageIndexProperty =
        PropertyHelper.CreateProperty<int, PaginatorPresenterPanel>(nameof(SelectedPageIndex), 0, (panel, _, _) => panel.InvalidateMeasure());

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SelectedPageIndex" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty TotalPagesProperty =
        PropertyHelper.CreateProperty<int, PaginatorPresenterPanel>(nameof(TotalPages), 0, (panel, _, _) => panel.InvalidateMeasure());

    protected override Size MeasureOverride(Size availableSize)
    {
        _pageSize = new Size();
        var pageCount = Math.Min(TotalPages, InternalChildren.Count);
        // Include offscreen labels so navigating across a digit boundary does not resize the buttons.
        for (var i = 0; i < pageCount; i++)
        {
            var child = InternalChildren[i];
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            _pageSize.Width = Math.Max(_pageSize.Width, child.DesiredSize.Width);
            _pageSize.Height = Math.Max(_pageSize.Height, child.DesiredSize.Height);
        }

        var (_, visibleCount) = OrientedImageryPagination.GetRange(pageCount, SelectedPageIndex, availableSize.Width, _pageSize.Width);
        return new Size(visibleCount * _pageSize.Width, _pageSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var pageCount = Math.Min(TotalPages, InternalChildren.Count);
        var (start, visibleCount) = OrientedImageryPagination.GetRange(pageCount, SelectedPageIndex, finalSize.Width, _pageSize.Width);
        var tabIndex = Math.Max(0, Math.Min(SelectedPageIndex, pageCount - 1));
        var left = Math.Max(0, (finalSize.Width - visibleCount * _pageSize.Width) / 2);
        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            bool visible = i >= start && i < start + visibleCount;
            if (visible)
            {
                var top = Math.Max(0, (finalSize.Height - child.DesiredSize.Height) / 2);
                child.Arrange(new Rect(left, top, _pageSize.Width, child.DesiredSize.Height));
                left += _pageSize.Width;
            }
            else
            {
                child.Arrange(new Rect(0, 0, 0, 0));
            }
            bool isTabStop = visible && i == tabIndex;
            if (child is Control childControl)
                childControl.IsTabStop = isTabStop;
            else if (child is ContentPresenter childPresenter)
                KeyboardNavigation.SetTabNavigation(childPresenter, isTabStop ? KeyboardNavigationMode.Continue : KeyboardNavigationMode.None);
        }

        return finalSize;
    }
}
