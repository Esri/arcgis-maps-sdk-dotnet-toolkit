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

#if __IOS__
using Foundation;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class FieldsTableGridHandler : LayoutHandler
{
    protected override LayoutView CreatePlatformView() => new FieldsTableGridView
    {
        CrossPlatformLayout = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set before creating the table view."),
    };

    protected override void DisconnectHandler(LayoutView platformView)
    {
        platformView.CrossPlatformLayout = null;
        platformView.View = null;
        base.DisconnectHandler(platformView);
    }
}

internal sealed class FieldsTableGridView : LayoutView, IUIAccessibilityContainer, IUIAccessibilityContainerDataTable
{
    internal FieldsTableGridView() => this.SetAccessibilityContainerType(UIAccessibilityContainerType.DataTable);

    private FieldsTableGrid? Table => CrossPlatformLayout as FieldsTableGrid;

    public nuint AccessibilityRowCount => (nuint)(Table?.RowCount ?? 0);

    public nuint AccessibilityColumnCount => (nuint)(Table?.ColumnCount ?? 0);

    public IUIAccessibilityContainerDataTableCell? GetAccessibilityDataTableCellElement(nuint row, nuint column)
    {
        var table = Table;
        if (table is null || row >= (nuint)table.RowCount || column >= (nuint)table.ColumnCount)
            return null;

        var cells = table.Cells[(int)row];
        var cell = column == 0 ? cells.Label : cells.Value;
        // Use the actual accessible text/link, not an extra focus stop around its native content.
        return cell.Content?.Handler?.PlatformView as IUIAccessibilityContainerDataTableCell;
    }
}

internal sealed class FieldsTableLabel : Label
{
}

internal sealed class FieldsTableLabelHandler : LabelHandler
{
    protected override MauiLabel CreatePlatformView() =>
        VirtualView is FieldsTableLabel { Parent: FieldsTableCell cell } ? new FieldsTableLabelView(cell) :
        throw new InvalidOperationException("The field table label must belong to a field table cell.");
}

internal sealed class FieldsTableLabelView : MauiLabel, IUIAccessibilityContainerDataTableCell
{
    private readonly FieldsTableCellAccessibility _cell;

    internal FieldsTableLabelView(FieldsTableCell cell) => _cell = new(cell);

    public NSRange GetAccessibilityRowRange() => _cell.RowRange;

    public NSRange GetAccessibilityColumnRange() => _cell.ColumnRange;

    public override string? AccessibilityLabel
    {
        get => _cell.GetLabel(base.AccessibilityLabel ?? Text);
        set => base.AccessibilityLabel = value;
    }
}

internal sealed class FieldsTableLinkView : UITextView, IUIAccessibilityContainerDataTableCell
{
    private readonly FieldsTableCellAccessibility _cell;

    internal FieldsTableLinkView(FieldsTableCell cell) => _cell = new(cell);

    public NSRange GetAccessibilityRowRange() => _cell.RowRange;

    public NSRange GetAccessibilityColumnRange() => _cell.ColumnRange;

    public override string? AccessibilityLabel
    {
        get => _cell.GetLabel(base.AccessibilityLabel ?? Text);
        set => base.AccessibilityLabel = value;
    }
}

internal sealed class FieldsTableCellAccessibility
{
    private readonly string _rowLabel;

    internal FieldsTableCellAccessibility(FieldsTableCell cell)
    {
        RowRange = new NSRange(cell.Row, 1);
        ColumnRange = new NSRange(cell.Column, 1);
        _rowLabel = cell.Column == 1 ? cell.RowLabel : string.Empty;
    }

    internal NSRange RowRange { get; }

    internal NSRange ColumnRange { get; }

    internal string? GetLabel(string? text) => string.IsNullOrEmpty(_rowLabel) ? text : $"{_rowLabel}: {text}";
}
#endif
