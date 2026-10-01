using Windows.Foundation;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

internal sealed partial class OrientedImageryToolbarPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0, rowWidth = 0, rowHeight = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
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
        return new Size(double.IsInfinity(availableSize.Width) ? Math.Max(width, rowWidth) : availableSize.Width, height + rowHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int start = 0;
        double top = 0, rowWidth = 0, rowHeight = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            var size = Children[i].DesiredSize;
            if (rowWidth > 0 && rowWidth + size.Width > finalSize.Width)
            {
                ArrangeRow(start, i, top, rowWidth, rowHeight, finalSize.Width);
                top += rowHeight;
                start = i;
                rowWidth = rowHeight = 0;
            }
            rowWidth += size.Width;
            rowHeight = Math.Max(rowHeight, size.Height);
        }
        ArrangeRow(start, Children.Count, top, rowWidth, rowHeight, finalSize.Width);
        return finalSize;
    }

    private void ArrangeRow(int start, int end, double top, double rowWidth, double rowHeight, double width)
    {
        double left = Math.Max(0, (width - rowWidth) / 2);
        for (int i = start; i < end; i++)
        {
            var size = Children[i].DesiredSize;
            Children[i].Arrange(new Rect(left, top + (rowHeight - size.Height) / 2, size.Width, size.Height));
            left += size.Width;
        }
    }
}
