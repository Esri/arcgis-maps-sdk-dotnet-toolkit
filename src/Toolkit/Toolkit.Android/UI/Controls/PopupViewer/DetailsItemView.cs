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

using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping.Popups;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class DetailsItemView : LinearLayout
    {
        private readonly TextView _label;
        private readonly TextView _formattedValue;

        internal DetailsItemView(Context context, Color foregroundColor)
            : base(context)
        {
            Orientation = Orientation.Vertical;
            LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            SetGravity(GravityFlags.Top);

            _label = new TextView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
            };
            _label.SetTextColor(Color.Argb(foregroundColor.A / 2, foregroundColor.R, foregroundColor.G, foregroundColor.B));
            AddView(_label);

            _formattedValue = new TextView(context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent),
            };
            _formattedValue.SetTextColor(foregroundColor);
            AddView(_formattedValue);
            RequestLayout();
        }

        internal void Update(PopupFieldValue field)
        {
            _label.Text = field?.Field?.Label;
            _formattedValue.Text = field?.FormattedValue;
        }
    }
}