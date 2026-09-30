using Esri.ArcGISRuntime.Toolkit.Internal;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

/// <summary>
/// A control representing a pagination bar for navigating pages.
/// </summary>
[TemplatePart(Name = PagesPresenterName, Type = typeof(ItemsControl))]
[TemplatePart(Name = LeftNavigateName, Type = typeof(Button))]
[TemplatePart(Name = RightNavigateName, Type = typeof(Button))]
public class Paginator : Control
{
    const string PagesPresenterName = "PART_PagesPresenter";
    const string LeftNavigateName = "PART_LeftNavigate";
    const string RightNavigateName = "PART_RightNavigate";

    // This list should be populated with as many ints as there are pages
    private int[] _presentedPages;

    private ItemsControl? _pagesPresenter;
    private Button? _leftNavigateButton;
    private Button? _rightNavigateButton;

    /// <summary>
    /// Creates a new instance of the <see cref="Paginator"/> class.
    /// </summary>
    public Paginator()
    {
        DefaultStyleKey = typeof(Paginator);
        _presentedPages = Array.Empty<int>();
    }

    /// <inheritdoc />
    public event EventHandler<Paginator, int>? SelectedPageIndexChanged;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_pagesPresenter != null)
        {
            _pagesPresenter.ItemsSource = null;
            _pagesPresenter.RemoveHandler(Button.ClickEvent, new RoutedEventHandler(PageNumberClicked));
        }
        if (_leftNavigateButton != null)
            _leftNavigateButton.Click -= LeftNavigateButton_Click;
        if (_rightNavigateButton != null)
            _rightNavigateButton.Click -= RightNavigateButton_Click;

        _pagesPresenter = GetTemplateChild(PagesPresenterName) as ItemsControl;
        _leftNavigateButton = GetTemplateChild(LeftNavigateName) as Button;
        _rightNavigateButton = GetTemplateChild(RightNavigateName) as Button;

        if (_pagesPresenter != null)
        {
            _pagesPresenter.ItemsSource = _presentedPages;
            _pagesPresenter.AddHandler(Button.ClickEvent, new RoutedEventHandler(PageNumberClicked));
        }
        if (_leftNavigateButton != null)
            _leftNavigateButton.Click += LeftNavigateButton_Click;
        if (_rightNavigateButton != null)
            _rightNavigateButton.Click += RightNavigateButton_Click;

        UpdateNavButtonsEnabled();
    }

    /// <summary>
    /// Gets or sets the index of the currently-selected page.
    /// </summary>
    public int SelectedPageIndex
    {
        get => (int)GetValue(CurrentPageNumberProperty);
        set => SetValue(CurrentPageNumberProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="SelectedPageIndex" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty CurrentPageNumberProperty =
        PropertyHelper.CreateProperty<int, Paginator>(nameof(SelectedPageIndex), 0, (paginator, oldValue, newValue) => paginator.OnCurrentPageNumberChanged(oldValue, newValue));


    private void OnCurrentPageNumberChanged(int oldValue, int newValue)
    {
        bool restorePageFocus = _pagesPresenter?.IsKeyboardFocusWithin == true;
        if (oldValue != newValue)
            SelectedPageIndexChanged?.Invoke(this, newValue);

        UpdateNavButtonsEnabled();
        if (restorePageFocus)
            Dispatcher.InvokeAsync(FocusSelectedPage, System.Windows.Threading.DispatcherPriority.Input);
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled || _pagesPresenter?.IsKeyboardFocusWithin != true || TotalPages <= 0 || Keyboard.Modifiers != ModifierKeys.None)
            return;

        int direction = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
        int index = e.Key switch
        {
            Key.Left => SelectedPageIndex - direction,
            Key.Right => SelectedPageIndex + direction,
            Key.Up => SelectedPageIndex - 1,
            Key.Down => SelectedPageIndex + 1,
            Key.Home => 0,
            Key.End => TotalPages - 1,
            _ => -1,
        };
        if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End))
            return;

        e.Handled = true;
        SetCurrentValue(CurrentPageNumberProperty, Math.Clamp(index, 0, TotalPages - 1));
    }

    private void FocusSelectedPage()
    {
        if (_pagesPresenter?.IsKeyboardFocusWithin != true)
            return;

        // Run after selection bindings update, without stealing focus if the user has already left.
        _pagesPresenter.UpdateLayout();
        if (_pagesPresenter?.ItemContainerGenerator.ContainerFromIndex(SelectedPageIndex) is FrameworkElement container && container.IsVisible)
            container.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="TotalPages" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty TotalPagesProperty =
        PropertyHelper.CreateProperty<int, Paginator>(nameof(TotalPages), 0, (s, oldValue, newValue) => s.OnTotalPagesChanged(oldValue, newValue));

    private void OnTotalPagesChanged(int oldValue, int newValue)
    {
        if (_presentedPages.Length == newValue)
            return;

        _presentedPages = new int[newValue];
        for (var i = 0; i < newValue; i++)
            _presentedPages[i] = i + 1;

        if (_pagesPresenter != null)
            _pagesPresenter.ItemsSource = _presentedPages;

        UpdateNavButtonsEnabled();
    }

    private void RightNavigateButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedPageIndex = Math.Min(TotalPages - 1, SelectedPageIndex + 1);
    }

    private void LeftNavigateButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedPageIndex = Math.Max(0, SelectedPageIndex - 1);
    }

    private void PageNumberClicked(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Button { DataContext: int pageNumber })
        {
            SelectedPageIndex = pageNumber - 1;
        }
    }

    private void UpdateNavButtonsEnabled()
    {
        if (_leftNavigateButton != null)
        {
            if (SelectedPageIndex < 1)
                _leftNavigateButton.IsEnabled = false;
            else
                _leftNavigateButton.IsEnabled = true;
        }

        if (_rightNavigateButton != null)
        {
            if (SelectedPageIndex >= TotalPages - 1)
                _rightNavigateButton.IsEnabled = false;
            else
                _rightNavigateButton.IsEnabled = true;
        }
    }
}

/// <summary>
/// Visibility converter for collapsing page button content unless currently on the selected page.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PaginatorCurrentPageVisibilityConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Length == 2 && values[0] is int pageNumber && values[1] is int currentPageIndex && pageNumber == currentPageIndex + 1
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Creates a localized accessible name identifying a page and its selection state.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PaginatorPageNameConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 3 || values[0] is not int page || values[1] is not int selectedIndex || values[2] is not int total)
            return DependencyProperty.UnsetValue;

        string key = page == selectedIndex + 1 ? "OrientedImageryViewSelectedPageFormat" : "OrientedImageryViewGoToPageFormat";
        return string.Format(culture, Properties.Resources.GetString(key)!, page, total);
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
