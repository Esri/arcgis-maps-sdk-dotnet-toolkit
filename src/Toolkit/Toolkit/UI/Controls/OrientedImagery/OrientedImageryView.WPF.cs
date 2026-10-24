#if WPF

using Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;
using System.Windows.Threading;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls;

[TemplatePart(Name = ImageViewportName, Type = typeof(OrientedImageViewport))]
[TemplatePart(Name = ToolbarContainerName, Type = typeof(ItemsControl))]
[TemplatePart(Name = PaginatorName, Type = typeof(Paginator))]
[TemplatePart(Name = CloseDetailsButtonName, Type = typeof(Button))]
public partial class OrientedImageryView : Control
{
    private const string CloseDetailsButtonName = "PART_CloseDetailsButton";

    private void FocusCloseDetailsButton()
    {
        if (ViewModel.SelectedImagePopup == null)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (ViewModel.SelectedImagePopup != null &&
                GetTemplateChild(CloseDetailsButtonName) is Button { IsVisible: true, IsEnabled: true } button)
            {
                button.Focus();
            }
        }));
    }
}

/// <summary>
/// Attached property to make sure the ComboBox always triggers an update even when clicking an already-selected option. This is used
/// in the oriented imagery view to make sure that sketch mode always gets disabled whenever you interact with the search mode selector.
/// </summary>
internal static class ImageSearchModeSelectionBehavior
{
    public static readonly DependencyProperty RepeatSelectionProperty = DependencyProperty.RegisterAttached(
        "RepeatSelection", typeof(bool), typeof(ImageSearchModeSelectionBehavior), new PropertyMetadata(false, OnRepeatSelectionChanged));

    public static bool GetRepeatSelection(DependencyObject element) => (bool)element.GetValue(RepeatSelectionProperty);

    public static void SetRepeatSelection(DependencyObject element, bool value) => element.SetValue(RepeatSelectionProperty, value);

    private static void OnRepeatSelectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ComboBoxItem item)
        {
            if ((bool)e.NewValue)
                item.PreviewMouseLeftButtonDown += Item_PreviewMouseLeftButtonDown;
            else
                item.PreviewMouseLeftButtonDown -= Item_PreviewMouseLeftButtonDown;
        }
    }

    private static void Item_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ComboBoxItem { IsSelected: true, DataContext: OrientedImageryViewInteractionState } item &&
            ItemsControl.ItemsControlFromItemContainer(item) is ComboBox { DataContext: ImageSearchModeVM viewModel })
        {
            viewModel.IsImageSelectionEnabled = true;
        }
    }
}

#endif