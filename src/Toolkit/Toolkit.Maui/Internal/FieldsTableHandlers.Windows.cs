// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *   http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using WinUIAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class FieldsTableGridHandler : LayoutHandler
{
    protected override LayoutPanel CreatePlatformView() => new FieldsTableGridPanel
    {
        CrossPlatformLayout = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set before creating the table panel."),
    };

    protected override void DisconnectHandler(LayoutPanel platformView)
    {
        // LayoutHandler clears the children but does not release the cross-platform layout.
        platformView.CrossPlatformLayout = null;
        base.DisconnectHandler(platformView);
    }
}

internal sealed class FieldsTableCellHandler : ContentViewHandler
{
    private static readonly IPropertyMapper<IContentView, IContentViewHandler> CellMapper =
        new PropertyMapper<IContentView, IContentViewHandler>(ContentViewHandler.Mapper)
        {
            [nameof(IContentView.Content)] = MapCellContent,
        };

    public FieldsTableCellHandler()
        : base(CellMapper)
    {
    }

    protected override ContentPanel CreatePlatformView() => new FieldsTableCellPanel
    {
        CrossPlatformLayout = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set before creating the cell panel."),
        IsHitTestVisible = true,
    };

    private static void MapCellContent(IContentViewHandler handler, IContentView view)
    {
        ContentViewHandler.MapContent(handler, view);
        if (view.PresentedContent is IView content && content.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
        {
            // Suppress the text container's duplicate announcement, not its inline hyperlinks.
            WinUIAutomationProperties.SetAccessibilityView(textBlock, AccessibilityView.Raw);
        }
    }
}

internal sealed partial class FieldsTableGridPanel : LayoutPanel
{
    internal FieldsTableGrid? Table => CrossPlatformLayout as FieldsTableGrid;

    protected override AutomationPeer OnCreateAutomationPeer() => new FieldsTableGridAutomationPeer(this);
}

internal sealed partial class FieldsTableCellPanel : ContentPanel
{
    internal FieldsTableCell? Cell => CrossPlatformLayout as FieldsTableCell;

    protected override AutomationPeer OnCreateAutomationPeer() => new FieldsTableCellAutomationPeer(this);
}
#endif
