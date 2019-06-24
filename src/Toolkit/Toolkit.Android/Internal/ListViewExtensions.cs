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

using Android.Views;
using Android.Widget;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class ListViewExtensions
    {
        internal static void SetHeightBasedOnChildren(this ListView listView)
        {
            if (listView.Adapter == null)
            {
                return;
            }

            int totalHeight = 0;
            int totalItems = listView.Adapter.Count;
            for (int i = 0; i < totalItems; i++)
            {
                var item = listView.Adapter.GetView(i, null, listView);
                if (item.Visibility == ViewStates.Gone)
                {
                    continue;
                }

                item.Measure(View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified), View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified));
                totalHeight += item.MeasuredHeight;
            }

            listView.LayoutParameters.Height = totalHeight + ((listView.DividerHeight * totalItems) - 1);
            listView.RequestLayout();
        }
    }
}