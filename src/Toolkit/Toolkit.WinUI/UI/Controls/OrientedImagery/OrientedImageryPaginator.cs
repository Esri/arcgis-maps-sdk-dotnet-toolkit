using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

internal sealed partial class OrientedImageryPaginator : Control
{
    private const string PagesPresenterName = "PART_PagesPresenter";
    private const string PreviousButtonName = "PART_PreviousPage";
    private const string NextButtonName = "PART_NextPage";
    private readonly ObservableCollection<OrientedImageryPage> _pages = new();
    private ItemsControl? _pagesPresenter;
    private Button? _previousButton;
    private Button? _nextButton;

    public OrientedImageryPaginator()
    {
        SizeChanged += (_, _) => UpdatePages();
    }

    public event EventHandler<OrientedImageryPaginator, int>? SelectedPageIndexChanged;

    public int SelectedPageIndex
    {
        get => (int)GetValue(SelectedPageIndexProperty);
        set => SetValue(SelectedPageIndexProperty, value);
    }

    public static readonly DependencyProperty SelectedPageIndexProperty =
        DependencyProperty.Register(nameof(SelectedPageIndex), typeof(int), typeof(OrientedImageryPaginator),
            new PropertyMetadata(-1, (sender, args) => ((OrientedImageryPaginator)sender).OnSelectedPageIndexChanged((int)args.NewValue)));

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(OrientedImageryPaginator),
            new PropertyMetadata(0, (sender, args) => ((OrientedImageryPaginator)sender).UpdatePages()));

    protected override void OnApplyTemplate()
    {
        if (_pagesPresenter != null)
        {
            _pagesPresenter.ItemsSource = null;
        }
        if (_previousButton != null)
            _previousButton.Click -= PreviousButton_Click;
        if (_nextButton != null)
            _nextButton.Click -= NextButton_Click;

        base.OnApplyTemplate();

        _pagesPresenter = GetTemplateChild(PagesPresenterName) as ItemsControl;
        _previousButton = GetTemplateChild(PreviousButtonName) as Button;
        _nextButton = GetTemplateChild(NextButtonName) as Button;

        if (_pagesPresenter != null)
        {
            _pagesPresenter.ItemsSource = _pages;
        }
        if (_previousButton != null)
            _previousButton.Click += PreviousButton_Click;
        if (_nextButton != null)
            _nextButton.Click += NextButton_Click;

        UpdatePages();
    }

    private void OnSelectedPageIndexChanged(int index)
    {
        UpdatePages();
        SelectedPageIndexChanged?.Invoke(this, index);
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e) => SelectedPageIndex = Math.Max(0, SelectedPageIndex - 1);

    private void NextButton_Click(object sender, RoutedEventArgs e) => SelectedPageIndex = Math.Min(TotalPages - 1, SelectedPageIndex + 1);

    private void UpdatePages()
    {
        if (_previousButton != null)
            _previousButton.IsEnabled = SelectedPageIndex > 0 && SelectedPageIndex < TotalPages;
        if (_nextButton != null)
            _nextButton.IsEnabled = SelectedPageIndex >= 0 && SelectedPageIndex < TotalPages - 1;

        var label = new TextBlock
        {
            Text = new string('8', Math.Max(1, TotalPages.ToString(System.Globalization.CultureInfo.CurrentCulture).Length)),
            FontSize = FontSize,
            FontFamily = FontFamily,
        };
        label.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var pageWidth = Math.Max(35, Math.Ceiling(label.DesiredSize.Width) + 16);
        // The outer control spans the available width; the centered inner group sizes to its contents.
        var width = Math.Max(0, ActualWidth - (_previousButton?.Width ?? 33) - (_nextButton?.Width ?? 33));
        var (start, count) = OrientedImageryPagination.GetRange(TotalPages, SelectedPageIndex, width, pageWidth);
        _pages.Clear();
        for (var i = start; i < start + count; i++)
        {
            var index = i;
            _pages.Add(new OrientedImageryPage(i + 1, pageWidth, i == SelectedPageIndex,
                new Command(() => SelectedPageIndex = index, () => true)));
        }
    }
}

internal sealed class OrientedImageryPage
{
    public OrientedImageryPage(int number, double width, bool isSelected, ICommand selectCommand)
    {
        Number = number;
        Width = width;
        SelectedIndicatorVisibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        SelectCommand = selectCommand;
    }

    public int Number { get; }
    public double Width { get; }
    public Visibility SelectedIndicatorVisibility { get; }
    public ICommand SelectCommand { get; }
}
