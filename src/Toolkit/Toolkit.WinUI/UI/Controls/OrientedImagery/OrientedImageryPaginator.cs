using Esri.ArcGISRuntime.Toolkit.Internal;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Windows.System;

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
    private bool _updatingPages;

    public OrientedImageryPaginator()
    {
        SizeChanged += (_, _) => UpdatePages();
        PreviewKeyDown += OnPreviewKeyDown;
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

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || GetFocusedPageButton() == null || TotalPages <= 0 ||
            IsKeyDown(VirtualKey.Control) || IsKeyDown(VirtualKey.Menu) || IsKeyDown(VirtualKey.Shift))
            return;

        int direction = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
        int? index = e.Key switch
        {
            VirtualKey.Left => SelectedPageIndex - direction,
            VirtualKey.Right => SelectedPageIndex + direction,
            VirtualKey.Up => SelectedPageIndex - 1,
            VirtualKey.Down => SelectedPageIndex + 1,
            VirtualKey.Home => 0,
            VirtualKey.End => TotalPages - 1,
            _ => null,
        };
        if (index.HasValue)
        {
            e.Handled = true;
            SelectedPageIndex = Math.Clamp(index.Value, 0, TotalPages - 1);
        }
    }

    private static bool IsKeyDown(VirtualKey key) =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    private Button? GetFocusedPageButton()
    {
        if (XamlRoot == null || _pagesPresenter == null)
            return null;

        var focusedButton = FocusManager.GetFocusedElement(XamlRoot) as Button;
        for (DependencyObject? element = focusedButton; element != null; element = VisualTreeHelper.GetParent(element))
        {
            if (element == _pagesPresenter)
                return focusedButton;
        }
        return null;
    }

    private static Button? FindPageButton(DependencyObject container)
    {
        if (container is Button button)
            return button;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
        {
            if (FindPageButton(VisualTreeHelper.GetChild(container, i)) is { } child)
                return child;
        }
        return null;
    }

    private void UpdatePages()
    {
        if (_updatingPages)
            return;

        _updatingPages = true;
        try
        {
            var focusedPage = GetFocusedPageButton();
            var focusState = focusedPage?.FocusState ?? FocusState.Unfocused;
            UpdatePageItems();
            if (focusedPage != null && _pagesPresenter != null && _pages.Count > 0)
            {
                // Realize the new range before restoring focus; do not queue work that could steal focus after Tab.
                _pagesPresenter.UpdateLayout();
                int selectedSlot = Math.Clamp(SelectedPageIndex, 0, TotalPages - 1) - (_pages[0].Number - 1);
                if (selectedSlot >= 0 && selectedSlot < _pages.Count &&
                    _pagesPresenter.ContainerFromIndex(selectedSlot) is DependencyObject container)
                    FindPageButton(container)?.Focus(focusState);
            }
        }
        finally
        {
            _updatingPages = false;
        }
    }

    private void UpdatePageItems()
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
        // Keep realized buttons alive across selection and range changes so keyboard focus is not discarded.
        for (var slot = 0; slot < count; slot++)
        {
            if (slot == _pages.Count)
            {
                var page = new OrientedImageryPage(index => SelectedPageIndex = index);
                page.Update(start + slot + 1, TotalPages, pageWidth, SelectedPageIndex);
                _pages.Add(page);
            }
            else
            {
                _pages[slot].Update(start + slot + 1, TotalPages, pageWidth, SelectedPageIndex);
            }
        }
        while (_pages.Count > count)
            _pages.RemoveAt(_pages.Count - 1);
    }
}

internal sealed class OrientedImageryPage : INotifyPropertyChanged
{
    public OrientedImageryPage(Action<int> selectPage)
    {
        SelectCommand = new Command(() => selectPage(Number - 1), () => true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Update(int number, int totalPages, double width, int selectedIndex)
    {
        bool isSelected = number == selectedIndex + 1;
        bool isTabStop = number == Math.Clamp(selectedIndex, 0, totalPages - 1) + 1;
        var visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        string key = isSelected ? "OrientedImageryViewSelectedPageFormat" : "OrientedImageryViewGoToPageFormat";
        string name = string.Format(CultureInfo.CurrentCulture, Properties.Resources.GetString(key)!, number, totalPages);
        if (Number == number && Width == width && SelectedIndicatorVisibility == visibility &&
            IsTabStop == isTabStop && AutomationName == name)
            return;

        Number = number;
        Width = width;
        SelectedIndicatorVisibility = visibility;
        IsTabStop = isTabStop;
        AutomationName = name;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public int Number { get; private set; }
    public double Width { get; private set; }
    public bool IsTabStop { get; private set; }
    public string AutomationName { get; private set; } = string.Empty;
    public Visibility SelectedIndicatorVisibility { get; private set; }
    public ICommand SelectCommand { get; }
}
