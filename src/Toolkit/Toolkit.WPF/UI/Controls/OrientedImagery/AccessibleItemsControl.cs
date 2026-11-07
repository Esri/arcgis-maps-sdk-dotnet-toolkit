using System.Windows.Automation.Peers;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.OrientedImagery;

/// <summary>
/// WPF seems not to be exposing some nested ItemsControls to the automation tree. Adding manual
/// automation peers to the nested ItemsControls seems to force it to show them.
/// </summary>
internal sealed class AccessibleItemsControl : ItemsControl
{
    protected override DependencyObject GetContainerForItemOverride() => new ToolbarContentPresenter();

    private sealed class ToolbarContentPresenter : ContentPresenter
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new ToolbarContentPresenterAutomationPeer(this);
    }

    private sealed class ToolbarContentPresenterAutomationPeer : FrameworkElementAutomationPeer
    {
        public ToolbarContentPresenterAutomationPeer(ToolbarContentPresenter owner)
            : base(owner)
        {
        }

        protected override bool IsControlElementCore() => false;

        protected override bool IsContentElementCore() => false;
    }
}
