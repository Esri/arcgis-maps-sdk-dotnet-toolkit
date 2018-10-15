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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class PopupFieldAdapter : BaseAdapter<PopupFieldValue>
    {
        private readonly IReadOnlyList<PopupFieldValue> _displayFields;
        private readonly Context _context;
        private Color _foregroundColor = Color.Black;

        internal PopupFieldAdapter(Context context, IEnumerable<PopupFieldValue> displayFields, Color foregroundColor)
        {
            _context = context;
            _displayFields = displayFields?.Any() ?? false ?
                new ReadOnlyCollection<PopupFieldValue>(displayFields?.ToList()) :
                new ReadOnlyCollection<PopupFieldValue>(Enumerable.Empty<PopupFieldValue>().ToList());
            _foregroundColor = foregroundColor;
        }

        internal void SetForegroundColor(Color foregroundColor)
        {
            _foregroundColor = foregroundColor;
        }

        public override PopupFieldValue this[int position] => _displayFields[position];

        public override int Count => _displayFields.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var popupFieldValue = _displayFields[position];
            if (convertView == null)
            {
                convertView = new DetailsItemView(_context, _foregroundColor);
            }

            (convertView as DetailsItemView)?.Update(popupFieldValue);
            return convertView;
        }
    }
}