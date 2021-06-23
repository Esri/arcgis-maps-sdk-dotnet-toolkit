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
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// View class used to render the individual entry for a <see cref="Bookmark" /> shown by <see cref="BookmarksAdapter" />.
    /// </summary>
    internal class BookmarkItemView : LinearLayout
    {
        // View that renders the bookmark's title.
        public TextView BookmarkLabel { get; }

        internal BookmarkItemView(Context? context)
            : base(context)
        {
            Orientation = Orientation.Horizontal;
            LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            SetGravity(GravityFlags.CenterVertical | GravityFlags.FillHorizontal);

            Clickable = true;

            BookmarkLabel = new TextView(context)
            {
                LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            };

            // Height
            var listItemHeightValue = new TypedValue();
            context?.Theme?.ResolveAttribute(Android.Resource.Attribute.ListPreferredItemHeight, listItemHeightValue, true);
            SetMinimumHeight((int)listItemHeightValue.GetDimension(Resources?.DisplayMetrics));

            // Left and right margin
            var listItemLeftMarginValue = new TypedValue();
            context?.Theme?.ResolveAttribute(Android.Resource.Attribute.ListPreferredItemPaddingStart, listItemLeftMarginValue, true);

            var listItemRightMarginValue = new TypedValue();
            context?.Theme?.ResolveAttribute(Android.Resource.Attribute.ListPreferredItemPaddingEnd, listItemRightMarginValue, true);
            SetPadding((int)listItemLeftMarginValue.GetDimension(Resources?.DisplayMetrics), 0, (int)listItemRightMarginValue.GetDimension(Resources?.DisplayMetrics), 0);

            BookmarkLabel.Gravity = GravityFlags.CenterVertical | GravityFlags.FillHorizontal;

            // Selection animation on hover
            var selectableBackground = new TypedValue();
            context?.Theme?.ResolveAttribute(Android.Resource.Attribute.SelectableItemBackground, selectableBackground, true);
            SetBackgroundResource(selectableBackground.ResourceId);

            AddView(BookmarkLabel);
        }
    }
}