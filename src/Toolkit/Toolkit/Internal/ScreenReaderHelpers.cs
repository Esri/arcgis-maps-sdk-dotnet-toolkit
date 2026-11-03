#if WPF
using System.Windows.Automation;
using System.Windows.Automation.Peers;
#elif WINUI
using Microsoft.UI.Xaml.Automation.Peers;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal;

internal static class ScreenReaderHelpers
{
#if WPF || WINUI
    /// <summary>
    /// Raises an <see cref="AutomationEvents.LiveRegionChanged"/> event for the given element.
    /// Will not raise an announcement if any of the following are true:
    ///
    /// <list type="number">
    ///   <item><paramref name="element"/> is <c>null</c></item>
    ///   <item><paramref name="element"/> is not visible after calling <see cref="UIElement.UpdateLayout()"/></item>
    ///   <item><paramref name="element"/> is a <see cref="ListView"/> with no items</item>
    /// </list>
    /// </summary>
    internal static void RaiseLiveRegionChanged(FrameworkElement? element)
    {
        if (element is null)
            return;

        element.UpdateLayout();

        if (element is ListView listView && listView.Items.Count == 0)
        {
            return;
        }

#if WPF
        if (!element.IsVisible)
        {
            return;
        }

        var peer = UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element);
#elif WINDOWS_XAML
            if (element.Visibility != Visibility.Visible)
            {
                return;
            }

            var peer = FrameworkElementAutomationPeer.FromElement(element) ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);
#endif
        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    /// <summary>
    /// Calls <see cref="AutomationPeer.RaiseNotificationEvent"/> event for the given element.
    /// Will not raise an announcement if any of the following are true:
    ///
    /// <list type="number">
    ///   <item><paramref name="element"/> is <c>null</c></item>
    ///   <item><paramref name="element"/> is not visible after calling <see cref="UIElement.UpdateLayout()"/></item>
    ///   <item><paramref name="element"/> is a <see cref="ListView"/> with no items</item>
    /// </list>
    /// </summary>
    internal static void RaiseNotificationAnnouncement(
        FrameworkElement? element,
        string announcement,
        string activityId,
        AutomationNotificationProcessing notificationProcessing = AutomationNotificationProcessing.MostRecent)
    {
        if (element is null)
            return;

        element.UpdateLayout();

        if (element is ListView listView && listView.Items.Count == 0)
        {
            return;
        }

#if WPF
        if (!element.IsVisible)
        {
            return;
        }

        var peer = UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element);
#elif WINDOWS_XAML
        if (element.Visibility != Visibility.Visible)
        {
            return;
        }

        var peer = FrameworkElementAutomationPeer.FromElement(element) ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);
#endif
        peer?.RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            notificationProcessing,
            announcement,
            activityId);
    }
#endif
}
