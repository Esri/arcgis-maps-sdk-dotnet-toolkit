using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

/// <summary>
/// Should hold a list of buttons. 
/// </summary>
internal class PaginatorPresenterPanel : Panel
{
    private int _visibleChildrenStart = 0;
    private int _visibleChildrenEnd = 0;
    private Size _requestedSize;

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
        var currentPage = SelectedPageIndex > -1 ? SelectedPageIndex : 0;
        var totalPagesCount = TotalPages;

        if (totalPagesCount < 1)
            return new Size(0, 0);

        InternalChildren[currentPage].Measure(availableSize);
        var desiredSize = InternalChildren[currentPage].DesiredSize;

        if (desiredSize.Width >= availableSize.Width)
        {
            _visibleChildrenStart = currentPage;
            _visibleChildrenEnd = currentPage;
            _requestedSize = desiredSize;
            return desiredSize;
        }

        var i = 1;
        while (true)
        {
            var lId = currentPage - i;
            var rId = currentPage + i;
            double additionalWidth = 0;
            double height = desiredSize.Height;

            if (lId >= 0)
            {
                InternalChildren[lId].Measure(availableSize);
                additionalWidth += InternalChildren[lId].DesiredSize.Width;
                height = Math.Max(height, InternalChildren[lId].DesiredSize.Height);
            }

            if (rId < totalPagesCount)
            {
                InternalChildren[rId].Measure(availableSize);
                additionalWidth += InternalChildren[rId].DesiredSize.Width;
                height = Math.Max(height, InternalChildren[rId].DesiredSize.Height);
            }

            if ((desiredSize.Width + additionalWidth >= availableSize.Width) || (lId < 0 && rId >= totalPagesCount))
            {
                _visibleChildrenStart = Math.Max(0, lId + 1);
                _visibleChildrenEnd = Math.Min(totalPagesCount, rId - 1);
                break;
            }

            desiredSize.Width += additionalWidth;
            desiredSize.Height = height;
            i++;
        }

        _requestedSize = desiredSize;
        return desiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var left = Math.Max(0, (finalSize.Width - _requestedSize.Width) / 2);
        for (var i = 0; i < TotalPages; i++)
        {
            var child = InternalChildren[i];
            if (i >= _visibleChildrenStart && i <= _visibleChildrenEnd)
            {
                var top = Math.Max(0, (finalSize.Height - child.DesiredSize.Height) / 2);
                child.Arrange(new Rect(new Point(left, top), child.DesiredSize));
                left += child.DesiredSize.Width;
                if (child is Control childControl)
                    childControl.IsTabStop = true;
                else if (child is ContentPresenter childPresenter)
                    KeyboardNavigation.SetTabNavigation(childPresenter, KeyboardNavigationMode.Once);
            }
            else
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                if (child is Control childControl)
                    childControl.IsTabStop = false;
                else if (child is ContentPresenter childPresenter)
                    KeyboardNavigation.SetTabNavigation(childPresenter, KeyboardNavigationMode.None);
            }
        }

        return _requestedSize;
    }
}
