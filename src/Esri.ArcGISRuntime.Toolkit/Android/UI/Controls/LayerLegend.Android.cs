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

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System.Collections.ObjectModel;
using System.Threading;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.LayerLegend")]
    public partial class LayerLegend
    {
        private static DisplayMetrics s_displayMetrics;
        private static IWindowManager s_windowManager;
        private LinearLayout _rootLayout;
        private ListView _listView;
        private SynchronizationContext _syncContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public LayerLegend(Context context) : base(context) { Initialize(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public LayerLegend(Context context, IAttributeSet attr) : base(context, attr) { Initialize(); }

        private void Initialize()
        {
            // TODO: Design time experience
            //if (DesignTime.IsDesignMode)
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _rootLayout = new LinearLayout(Context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };
            _rootLayout.SetGravity(GravityFlags.Top);

            _listView = new ListView(Context);
            _rootLayout.AddView(_listView);
            
            // Add root layout to view
            AddView(_rootLayout);
            _rootLayout.RequestLayout();
        }

        private void Refresh()
        {
            if (_listView == null)
                return;
            if (LayerContent == null)
                _listView.Adapter = null;
           
            if (LayerContent is ILoadable)
            {
                if ((LayerContent as ILoadable).LoadStatus != LoadStatus.Loaded)
                {
                    (LayerContent as ILoadable).Loaded += Layer_Loaded;
                    return;
                }
            }

            var items = new ObservableCollection<LayerLegendInfo>();
            LoadRecursive(items, LayerContent, ShowEntireTreeHierarchy);
            _listView.Adapter = new LegendsAdapter(Context, items);
        }

        private void Layer_Loaded(object sender, System.EventArgs e)
        {
            (sender as ILoadable).Loaded -= Layer_Loaded;            
            _syncContext?.Post(_ => Refresh(), null);
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

        // Gets a display metrics object for calculating display dimensions
        private static DisplayMetrics GetDisplayMetrics()
        {
            if (s_displayMetrics == null)
            {
                if (s_windowManager == null)
                    s_windowManager = Application.Context?.GetSystemService(Context.WindowService)?.JavaCast<IWindowManager>();
                if (s_windowManager == null)
                {
                    s_displayMetrics = Application.Context?.Resources?.DisplayMetrics;
                }
                else
                {
                    s_displayMetrics = new DisplayMetrics();
                    s_windowManager.DefaultDisplay.GetMetrics(s_displayMetrics);
                }
            }
            return s_displayMetrics;
        }

        // Calculates a screen dimension given a specified dimension in raw pixels
        private float CalculateScreenDimension(float pixels, ComplexUnitType screenUnitType = ComplexUnitType.Dip)
        {
            return !DesignTime.IsDesignMode ?
                TypedValue.ApplyDimension(screenUnitType, pixels, GetDisplayMetrics()) : pixels;
        }
    }
}