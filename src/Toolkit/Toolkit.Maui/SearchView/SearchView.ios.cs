#if __IOS__ || MACCATALYST
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui;

public partial class SearchView
{
    private UIView? _nativeSearchView;
    private bool _nativeSearchViewOriginallyGroupedChildren;

    partial void ConnectKeyboardNavigation()
    {
        SetHandlerChangedSubscriptions(true);
        WireNativeControls();
    }

    partial void DisconnectKeyboardNavigation()
    {
        SetHandlerChangedSubscriptions(false);
        UnwireNativeControls();
    }

    partial void OnSourceListOpened() =>
        Dispatcher.Dispatch(() => Microsoft.Maui.Accessibility.SemanticScreenReader.Default.Announce(
            Properties.Resources.GetString("SearchViewSourcesAvailable")));

    private void SetHandlerChangedSubscriptions(bool subscribe)
    {
        foreach (var part in new VisualElement?[]
        {
            PART_SourcesView,
            PART_SuggestionsView,
            PART_ResultView,
        })
        {
            if (part == null)
            {
                continue;
            }

            if (subscribe)
            {
                part.HandlerChanged += TemplatePart_HandlerChanged;
            }
            else
            {
                part.HandlerChanged -= TemplatePart_HandlerChanged;
            }
        }
    }

    private void TemplatePart_HandlerChanged(object? sender, EventArgs e) => WireNativeControls();

    private void WireNativeControls()
    {
        UnwireNativeControls();

        _nativeSearchView = Handler?.PlatformView as UIView;

        if (_nativeSearchView != null)
        {
            // Keep each child focusable while asking assistive technologies to traverse the SearchView as one group.
            _nativeSearchViewOriginallyGroupedChildren = _nativeSearchView.ShouldGroupAccessibilityChildren;
            _nativeSearchView.ShouldGroupAccessibilityChildren = true;
        }
    }

    private void UnwireNativeControls()
    {
        if (_nativeSearchView != null)
        {
            _nativeSearchView.ShouldGroupAccessibilityChildren = _nativeSearchViewOriginallyGroupedChildren;
        }

        _nativeSearchView = null;
        _nativeSearchViewOriginallyGroupedChildren = false;
    }
}
#endif
