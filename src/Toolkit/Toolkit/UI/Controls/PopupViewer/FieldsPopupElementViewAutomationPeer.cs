// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/
#if WPF
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
#elif WINUI
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
#endif

#if WPF || WINUI
namespace Esri.ArcGISRuntime.Toolkit.Primitives
{
    /// <summary>
    /// Exposes the two-column (label, value) table built by <see cref="FieldsPopupElementView"/> as a UIA
    /// Grid/Table, so Narrator and other assistive technology can navigate it like a table instead of a
    /// generic panel of unrelated text.
    /// </summary>
    internal sealed partial class FieldsPopupElementViewAutomationPeer : FrameworkElementAutomationPeer, IGridProvider, ITableProvider
    {
        public FieldsPopupElementViewAutomationPeer(FieldsPopupElementView owner)
            : base(owner)
        {
        }

        private FieldsPopupElementView TableView => (FieldsPopupElementView)Owner;

#if WPF
        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Grid || patternInterface == PatternInterface.Table)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }
#elif WINUI
        /// <inheritdoc />
        protected override object GetPatternCore(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Grid || patternInterface == PatternInterface.Table)
            {
                return this;
            }
            return base.GetPatternCore(patternInterface);
        }
#endif

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(FieldsPopupElementView);

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table;

        /// <inheritdoc />
        public int RowCount => TableView.Cells.Count;

        /// <inheritdoc />
        public int ColumnCount => 2;

        /// <inheritdoc />
        public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;

        /// <inheritdoc />
        public IRawElementProviderSimple GetItem(int row, int column)
        {
            var cells = TableView.Cells;
            if (row < 0 || row >= cells.Count || column < 0 || column > 1)
            {
                return null!;
            }
            var element = column == 0 ? cells[row].Label : cells[row].Value;
#if WPF
            var peer = UIElementAutomationPeer.CreatePeerForElement(element) ?? UIElementAutomationPeer.FromElement(element);
#elif WINUI
            var peer = FrameworkElementAutomationPeer.FromElement(element);
#endif
            return peer is null ? null! : ProviderFromPeer(peer);
        }

        // There's no dedicated header row/column in this layout (every row has its own label cell instead),
        // so per-item headers are exposed via AutomationProperties.LabeledBy (already set in RefreshTable)
        // rather than ITableProvider's fixed header collections.
        /// <inheritdoc />
        public IRawElementProviderSimple[]? GetRowHeaders() => null;

        /// <inheritdoc />
        public IRawElementProviderSimple[]? GetColumnHeaders() => null;
    }

    // Implemented by the label/value cell elements created in FieldsPopupElementView.RefreshTable() (set
    // immediately after creation) so their own automation peers - FieldsTableCellAutomationPeer below - can
    // report Grid/Table row+column position back to UIA. Without this, FieldsPopupElementViewAutomationPeer.GetItem
    // hands back a cell whose peer has no way to answer "what row/column am I," so Narrator's table
    // navigation (Ctrl+Alt+Arrow) can't move cell-to-cell even though the container correctly reports itself as a table.
    internal interface IFieldsTableCell
    {
        int Row { get; set; }
        int Column { get; set; }
        FrameworkElement? ContainingGridElement { get; set; }
        FrameworkElement? RowHeaderElement { get; set; }
    }

#if WPF
    internal sealed class FieldsTableCellTextBlock : TextBlock, IFieldsTableCell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public FrameworkElement? ContainingGridElement { get; set; }
        public FrameworkElement? RowHeaderElement { get; set; }

        protected override AutomationPeer OnCreateAutomationPeer() => new FieldsTableCellAutomationPeer(this);
    }

    internal sealed class FieldsTableCellTextBox : TextBox, IFieldsTableCell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public FrameworkElement? ContainingGridElement { get; set; }
        public FrameworkElement? RowHeaderElement { get; set; }

        protected override AutomationPeer OnCreateAutomationPeer() => new FieldsTableCellAutomationPeer(this);
    }
#elif WINUI
    // Microsoft.UI.Xaml.Controls.TextBlock (and most other leaf controls, e.g. Border) is sealed in WinUI, so
    // unlike the WPF version above this can't subclass TextBlock directly to override OnCreateAutomationPeer().
    // Instead it wraps a TextBlock as its single child (Grid isn't sealed) and forwards the handful of members
    // the table-building code in FieldsPopupElementView.cs actually uses.
    internal sealed partial class FieldsTableCellTextBlock : Microsoft.UI.Xaml.Controls.Grid, IFieldsTableCell
    {
        internal readonly Microsoft.UI.Xaml.Controls.TextBlock InnerTextBlock = new();

        public FieldsTableCellTextBlock()
        {
            Children.Add(InnerTextBlock);

            // InnerTextBlock is a real visual element with its own default TextBlockAutomationPeer, which UIA
            // discovers on its own as a child of this wrapper - even though FieldsTableCellAutomationPeer below
            // computes the correct folded Name for the *cell*, InnerTextBlock's own peer was still showing up as
            // an extra, unsuppressed stop repeating the same text a second time (confirmed live: Narrator read
            // the cell, then immediately read it again). Raw removes InnerTextBlock's own peer from the
            // Control/Content views entirely; any of its own automation-relevant descendants (e.g. the
            // Hyperlink inline in CreateHyperlinkCell) still surface normally since UIA promotes a hidden node's
            // visible descendants up to the nearest visible ancestor instead of hiding the whole subtree.
            AutomationProperties.SetAccessibilityView(InnerTextBlock, AccessibilityView.Raw);
        }

        public string Text
        {
            get => InnerTextBlock.Text;
            set => InnerTextBlock.Text = value;
        }

        public Microsoft.UI.Xaml.TextWrapping TextWrapping
        {
            get => InnerTextBlock.TextWrapping;
            set => InnerTextBlock.TextWrapping = value;
        }

        public Microsoft.UI.Xaml.Documents.InlineCollection Inlines => InnerTextBlock.Inlines;

        public new Microsoft.UI.Xaml.Style? Style
        {
            get => InnerTextBlock.Style;
            set => InnerTextBlock.Style = value;
        }

        public int Row { get; set; }
        public int Column { get; set; }
        public FrameworkElement? ContainingGridElement { get; set; }
        public FrameworkElement? RowHeaderElement { get; set; }

        protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new FieldsTableCellAutomationPeer(this);
    }
#endif

    /// <summary>
    /// Automation peer for one cell (label or value) in the table built by <see cref="FieldsPopupElementView"/>,
    /// reporting its row/column position via <see cref="IGridItemProvider"/>/<see cref="ITableItemProvider"/> so
    /// Narrator's table navigation commands work cell-by-cell instead of just recognizing the container as a table.
    /// </summary>
    internal sealed partial class FieldsTableCellAutomationPeer : FrameworkElementAutomationPeer, IGridItemProvider, ITableItemProvider
    {
        public FieldsTableCellAutomationPeer(FrameworkElement owner)
            : base(owner)
        {
        }

        private IFieldsTableCell Cell => (IFieldsTableCell)Owner;

        private static string GetOwnText(FrameworkElement element)
        {
#if WPF
            if (element is System.Windows.Controls.TextBox tb) return tb.Text ?? string.Empty;
            if (element is System.Windows.Controls.TextBlock tblk) return tblk.Text ?? string.Empty;
#elif WINUI
            if (element is FieldsTableCellTextBlock tblk) return tblk.Text ?? string.Empty;
#endif
            return string.Empty;
        }

        // Explicit, deterministic Name/ControlType for this cell. Without this, GetNameCore() falls back to
        // whatever WPF computes automatically for a table-item peer with a row header (GetRowHeaderItems()
        // below) - which turns out to be the ROW HEADER'S name, not this cell's own text - and
        // GetAutomationControlTypeCore() falls back to the generic AutomationControlType.Custom.
        //
        // Value cells (column 1) fold their row's label into their own Name (e.g. "Name: White Mountain Peak"),
        // since the label cell (column 0) is excluded from normal navigation below - this collapses each row
        // into a single stop instead of two, while still speaking the label. The row number itself is *not*
        // baked in here - Narrator already announces "Row X of Y" on its own from the IGridItemProvider/
        // ContainingGrid pattern data, so adding it here would just say it twice.
        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var ownText = GetOwnText((FrameworkElement)Owner);
            if (Cell.Column == 1 && Cell.RowHeaderElement is FrameworkElement header)
            {
                var labelText = GetOwnText(header);
                if (!string.IsNullOrEmpty(labelText))
                {
                    return $"{labelText}: {ownText}";
                }
            }
            return ownText;
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        // Label cells (column 0) are no longer independent stops for normal Tab/arrow-key navigation - their
        // text is folded into the value cell's Name instead (see GetNameCore()). They remain reachable via
        // explicit table-navigation commands (Ctrl+Alt+Arrow) through GetItem(row, 0), since that's a
        // separate mechanism from the control/content view these two flags govern.
        /// <inheritdoc />
        protected override bool IsControlElementCore() => Cell.Column != 0;

        /// <inheritdoc />
        protected override bool IsContentElementCore() => Cell.Column != 0;

#if WPF
        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.GridItem || patternInterface == PatternInterface.TableItem)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }
#elif WINUI
        /// <inheritdoc />
        protected override object GetPatternCore(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.GridItem || patternInterface == PatternInterface.TableItem)
            {
                return this;
            }
            return base.GetPatternCore(patternInterface);
        }
#endif

        /// <inheritdoc />
        public int Row => Cell.Row;

        /// <inheritdoc />
        public int Column => Cell.Column;

        /// <inheritdoc />
        public int RowSpan => 1;

        /// <inheritdoc />
        public int ColumnSpan => 1;

        /// <inheritdoc />
        public IRawElementProviderSimple ContainingGrid
        {
            get
            {
                var grid = Cell.ContainingGridElement;
                if (grid is null)
                {
                    return null!;
                }
#if WPF
                var peer = UIElementAutomationPeer.CreatePeerForElement(grid) ?? UIElementAutomationPeer.FromElement(grid);
#elif WINUI
                var peer = FrameworkElementAutomationPeer.FromElement(grid);
#endif
                return peer is null ? null! : ProviderFromPeer(peer);
            }
        }

        // Deliberately returns null: GetNameCore() above already folds the row header's text directly into
        // this cell's own Name, so returning it here too would make Narrator announce it a second time
        // ("Row Header: Name") on top of the Name it already just read.
        /// <inheritdoc />
        public IRawElementProviderSimple[]? GetRowHeaderItems() => null;

        /// <inheritdoc />
        public IRawElementProviderSimple[]? GetColumnHeaderItems() => null;
    }
}
#endif
