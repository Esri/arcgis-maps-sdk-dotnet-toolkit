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
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System.Runtime.InteropServices;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal abstract partial class FieldsTableAutomationPeer : FrameworkElementAutomationPeer
{
    private const int ElementNotAvailableHResult = unchecked((int)0x80040201); // UIA_E_ELEMENTNOTAVAILABLE

    protected FieldsTableAutomationPeer(MauiPanel owner)
        : base(owner)
    {
    }

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;

    protected static COMException ElementNotAvailable() =>
        new("The field table element is no longer available.", ElementNotAvailableHResult);

    protected IRawElementProviderSimple GetProvider(View view)
    {
        if (view.Handler?.PlatformView is not MauiPanel panel || !ReferenceEquals(panel.CrossPlatformLayout, view))
        {
            throw ElementNotAvailable();
        }

        var peer = FromElement(panel) as FieldsTableAutomationPeer ?? throw ElementNotAvailable();
        return ProviderFromPeer(peer) ?? throw ElementNotAvailable();
    }
}

internal sealed partial class FieldsTableGridAutomationPeer : FieldsTableAutomationPeer, IGridProvider, ITableProvider
{
    internal FieldsTableGridAutomationPeer(FieldsTableGridPanel owner)
        : base(owner)
    {
    }

    private FieldsTableGrid Table => ((FieldsTableGridPanel)Owner).Table ?? throw ElementNotAvailable();

    protected override string GetClassNameCore() => nameof(FieldsTableGrid);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table;

    protected override object GetPatternCore(PatternInterface patternInterface) =>
        patternInterface is PatternInterface.Grid or PatternInterface.Table ? this : base.GetPatternCore(patternInterface);

    public int RowCount => Table.RowCount;

    public int ColumnCount => Table.ColumnCount;

    public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;

    public IRawElementProviderSimple[]? GetRowHeaders() => null;

    public IRawElementProviderSimple[]? GetColumnHeaders() => null;

    public IRawElementProviderSimple GetItem(int row, int column)
    {
        var table = Table;
        if (row < 0 || row >= table.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
        if (column < 0 || column >= table.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        var cells = table.Cells[row];
        return GetProvider(column == 0 ? cells.Label : cells.Value);
    }
}

internal sealed partial class FieldsTableCellAutomationPeer : FieldsTableAutomationPeer, IGridItemProvider, ITableItemProvider
{
    internal FieldsTableCellAutomationPeer(FieldsTableCellPanel owner)
        : base(owner)
    {
    }

    private FieldsTableCell Cell => ((FieldsTableCellPanel)Owner).Cell ?? throw ElementNotAvailable();

    protected override string GetClassNameCore() => nameof(FieldsTableCell);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    protected override string GetNameCore()
    {
        var cell = Cell;
        var text = cell.Content is Label label ? label.Text ?? label.FormattedText?.ToString() ?? string.Empty : base.GetNameCore();
        return cell.Column == 1 && !string.IsNullOrEmpty(cell.RowLabel) ? $"{cell.RowLabel}: {text}" : text;
    }

    // Label cells remain reachable through Grid.GetItem, but are not separate ordinary navigation stops.
    protected override bool IsControlElementCore() => Cell.Column != 0;

    protected override bool IsContentElementCore() => Cell.Column != 0;

    protected override object GetPatternCore(PatternInterface patternInterface) =>
        patternInterface is PatternInterface.GridItem or PatternInterface.TableItem ? this : base.GetPatternCore(patternInterface);

    public int Row => Cell.Row;

    public int Column => Cell.Column;

    public int RowSpan => 1;

    public int ColumnSpan => 1;

    public IRawElementProviderSimple ContainingGrid => GetProvider(Cell.ContainingTable);

    // The row label is already included in the value-cell name, so exposing it as a header would repeat it.
    public IRawElementProviderSimple[]? GetRowHeaderItems() => null;

    public IRawElementProviderSimple[]? GetColumnHeaderItems() => null;
}
#endif
