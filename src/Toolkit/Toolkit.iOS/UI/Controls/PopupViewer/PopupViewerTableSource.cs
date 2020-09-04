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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Esri.ArcGISRuntime.Mapping.Popups;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class PopupViewerTableSource : UITableViewSource
    {
        private readonly IReadOnlyList<PopupFieldValue> _displayFields;
        internal static readonly NSString CellId = new NSString(nameof(DetailsItemCell));
        private UIColor _foregroundColor = UIColorHelper.LabelColor;

        public PopupViewerTableSource(IEnumerable<PopupFieldValue> displayFields, UIColor foregroundColor)
            : base()
        {
            _displayFields = displayFields?.Any() ?? false ?
                new ReadOnlyCollection<PopupFieldValue>(displayFields?.ToList()) :
                new ReadOnlyCollection<PopupFieldValue>(Enumerable.Empty<PopupFieldValue>().ToList());
            _foregroundColor = foregroundColor;
        }

        internal void SetForegroundColor(UIColor foregroundColor)
        {
            _foregroundColor = foregroundColor;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return _displayFields?.Count ?? 0;
        }

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
        {
            return UITableView.AutomaticDimension;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var info = _displayFields[indexPath.Row];
            var cell = tableView.DequeueReusableCell(CellId) as DetailsItemCell;
            if (cell == null)
            {
                return null;
            }

            cell.LayoutMargins = UIEdgeInsets.Zero;
            cell.SetForegroundColor(_foregroundColor);
            cell.Update(info);
            cell.SetNeedsUpdateConstraints();
            cell.UpdateConstraints();
            return cell;
        }
    }
}