// /*******************************************************************************
//  * Copyright 2017 Esri
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
using Android.Views;
using Android.Widget;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendView : LinearLayout
    {
        private SymbolDisplay _symbolDisplay;
        private TextView _textView;

        internal LegendView(Context context) : base(context)
        {
            Orientation = Orientation.Horizontal;
            LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            SetGravity(GravityFlags.Top);

            _symbolDisplay = new SymbolDisplay(context);
            AddView(_symbolDisplay);
            _textView = new TextView(context)
            {
                TextAlignment = Android.Views.TextAlignment.Center
            };
            AddView(_textView);
        }

        internal void Update(LayerLegendInfo info)
        {
            _symbolDisplay.Symbol = info?.Symbol;
            _textView.Text = info?.Name;
        }
    }
}