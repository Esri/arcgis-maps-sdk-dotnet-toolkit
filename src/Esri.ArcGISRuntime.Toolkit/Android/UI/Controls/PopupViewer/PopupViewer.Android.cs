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
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
     /// The PopupViewer control is used to display details and media, edit attributes, geometry and related records,
     /// manage attachments of an <see cref="Data.ArcGISFeature"/> or a <see cref="ArcGISRuntime.UI.Graphic"/>
     /// as defined in its <see cref="Mapping.Popups.Popup"/>.
     /// </summary>
    public partial class PopupViewer
    {
        private LinearLayout _rootLayout;
        private TextView _editSummary;
        private TextView _customHtmlDescription;
        private ListView _detailsList;

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupViewer"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public PopupViewer(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupViewer"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public PopupViewer(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        internal void Initialize()
        {
            if (!DesignTime.IsDesignMode)
            {
                return;
            }

            _rootLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };
            _rootLayout.SetGravity(GravityFlags.Top);

            _editSummary = new TextView(Context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };
            _rootLayout.AddView(_editSummary);

            _customHtmlDescription = new TextView(Context)
            {
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };
            _rootLayout.AddView(_customHtmlDescription);

            _detailsList = new ListView(Context)
            {
                ClipToOutline = true,
                Clickable = false,
                ChoiceMode = ChoiceMode.None,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
                ScrollingCacheEnabled = false,
                PersistentDrawingCache = PersistentDrawingCaches.NoCache,
            };

            _rootLayout.AddView(_detailsList);
            _rootLayout.RequestLayout();
        }

        private void Refresh()
        {
            if (_detailsList == null)
            {
                return;
            }

            if (PopupManager == null)
            {
                _detailsList.Adapter = null;
                return;
            }

            _editSummary.Visibility = PopupManager.ShowEditSummary ? ViewStates.Visible : ViewStates.Gone;
            _editSummary.Text = PopupManager.EditSummary;

            var displayDescription = PopupManager.ShowCustomDescriptionHtml || !string.IsNullOrWhiteSpace(PopupManager.CustomDescriptionHtml);
            _customHtmlDescription.Visibility = displayDescription ? ViewStates.Visible : ViewStates.Gone;
            _customHtmlDescription.Text = PopupManager.CustomDescriptionHtml;

            if (!displayDescription)
            {
                _detailsList.Adapter = new PopupFieldAdapter(Context, PopupManager.DisplayedFields);
                _detailsList.SetHeightBasedOnChildren();
            }
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            // Initialize dimensions of root layout
            MeasureChild(_rootLayout, widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

            // Calculate the ideal width and height for the view
            var desiredWidth = PaddingLeft + PaddingRight + _rootLayout.MeasuredWidth;
            var desiredHeight = PaddingTop + PaddingBottom + _rootLayout.MeasuredHeight;

            // Get the width and height of the view given any width and height constraints indicated by the width and height spec values
            var width = ResolveSize(desiredWidth, widthMeasureSpec);
            var height = ResolveSize(desiredHeight, heightMeasureSpec);
            SetMeasuredDimension(width, height);
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // Forward layout call to the root layout
            _rootLayout.Layout(PaddingLeft, PaddingTop, _rootLayout.MeasuredWidth + PaddingLeft, _rootLayout.MeasuredHeight + PaddingBottom);
        }
    }
}