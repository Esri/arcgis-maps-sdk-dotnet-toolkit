// /*******************************************************************************
//  * Copyright 2026 Esri
//  *
//  * Licensed under the Apache License, Version 2.0 (the "License");
//  * you may not use this file except in compliance with the License.
//  * You may obtain a copy of the License at
//  *
//  * http://www.apache.org/licenses/LICENSE-2.0
//  *
//  * Unless required by applicable law or agreed to in writing, software
//  * distributed under the License is distributed on an "AS IS" BASIS,
//  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  * See the License for the specific language governing permissions and
//  * limitations under the License.
//  ******************************************************************************/

#if __ANDROID__
using Android.Content;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View.Accessibility;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class FieldsTableGridHandler : LayoutHandler
{
    protected override LayoutViewGroup CreatePlatformView()
    {
        var view = new FieldsTableGridView(Context)
        {
            CrossPlatformLayout = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set before creating the table view."),
        };
        view.SetClipChildren(false);
        return view;
    }

    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        PlatformView.ImportantForAccessibility = ImportantForAccessibility.Yes;
    }

    protected override void DisconnectHandler(LayoutViewGroup platformView)
    {
        platformView.CrossPlatformLayout = null;
        base.DisconnectHandler(platformView);
    }
}

internal sealed class FieldsTableGridView : LayoutViewGroup
{
    internal FieldsTableGridView(Context context) : base(context)
    {
    }

    public override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo? info)
    {
        base.OnInitializeAccessibilityNodeInfo(info);
        if (info is not null && CrossPlatformLayout is FieldsTableGrid table)
        {
            // Keep even a single-row table recognizable as a grid, rather than a list.
            info.ClassName = "android.widget.GridView";
            using var node = AccessibilityNodeInfoCompat.Wrap(info);
            node.SetCollectionInfo(AccessibilityNodeInfoCompat.CollectionInfoCompat.Obtain(table.RowCount, table.ColumnCount, false));
        }
    }
}

internal sealed class FieldsTableLabel : Label
{
}

internal sealed class FieldsTableLabelHandler : LabelHandler
{
    protected override AppCompatTextView CreatePlatformView() => new FieldsTableLabelView(Context);

    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);
        ((FieldsTableLabelView)PlatformView).Label = (FieldsTableLabel)view;
        PlatformView.ImportantForAccessibility = ImportantForAccessibility.Yes;
    }

    protected override void DisconnectHandler(AppCompatTextView platformView)
    {
        ((FieldsTableLabelView)platformView).Label = null;
        base.DisconnectHandler(platformView);
    }
}

internal sealed class FieldsTableLabelView : MauiTextView
{
    internal FieldsTableLabelView(Context context) : base(context)
    {
    }

    internal FieldsTableLabel? Label { get; set; }

    public override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo? info)
    {
        base.OnInitializeAccessibilityNodeInfo(info);
        if (info is not null && Label?.Parent is FieldsTableCell cell && ReferenceEquals(cell.Content, Label))
        {
            // Empty cells need accessibility focus, not an additional keyboard stop.
            info.ScreenReaderFocusable = true;
            using var node = AccessibilityNodeInfoCompat.Wrap(info);
            node.SetCollectionItemInfo(AccessibilityNodeInfoCompat.CollectionItemInfoCompat.Obtain(cell.Row, 1, cell.Column, 1, false, false));
            if (cell.Column == 1 && !string.IsNullOrEmpty(cell.RowLabel))
            {
                node.SetLabeledBy(null);
                info.ContentDescription = $"{cell.RowLabel}: {info.ContentDescription ?? info.Text}";
            }
        }
    }
}
#endif
