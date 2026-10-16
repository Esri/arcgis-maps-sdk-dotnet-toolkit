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

namespace Esri.ArcGISRuntime.Toolkit.Maui.Internal;

internal sealed class FieldsTableGrid : Grid
{
    private readonly List<(FieldsTableCell Label, FieldsTableCell Value)> _cells = new();

    internal IReadOnlyList<(FieldsTableCell Label, FieldsTableCell Value)> Cells => _cells;

    internal int RowCount => _cells.Count;

    internal int ColumnCount => 2;

    internal void AddRow(View label, View value, string? rowLabel)
    {
        int row = RowCount;
        var labelCell = new FieldsTableCell(this, row, 0, rowLabel, label);
        var valueCell = new FieldsTableCell(this, row, 1, rowLabel, value);

        RowDefinitions.Add(new RowDefinition());
        Grid.SetRow(labelCell, row);
        Grid.SetRow(valueCell, row);
        // Visual column 1 is the divider; logical column 1 is the value cell.
        Grid.SetColumn(valueCell, 2);
        Children.Add(labelCell);
        Children.Add(valueCell);
        _cells.Add((labelCell, valueCell));
    }
}

internal sealed class FieldsTableCell : ContentView
{
    internal FieldsTableCell(FieldsTableGrid containingTable, int row, int column, string? rowLabel, View content)
    {
        ContainingTable = containingTable;
        Row = row;
        Column = column;
        RowLabel = rowLabel ?? string.Empty;
        Content = content;
    }

    internal FieldsTableGrid ContainingTable { get; }

    internal int Row { get; }

    internal int Column { get; }

    internal string RowLabel { get; }
}
