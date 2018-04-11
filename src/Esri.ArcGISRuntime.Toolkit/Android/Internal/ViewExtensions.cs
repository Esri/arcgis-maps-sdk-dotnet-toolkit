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
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Helper class for providing common cross-platform names for UI component manipulation
    /// </summary>
    internal static class ViewExtensions
    {
        public static void SetMargin(this View view, double left, double top, double right, double bottom)
        {
            throw new NotImplementedException();
        }

        public static double GetActualWidth(this View view)
        {
            throw new NotImplementedException();
        }

        public static void SetWidth(this RectangleView view, double width)
        {
            if (view == null)
            {
                return;
            }

            view.Width = width;
        }

        public static bool GetIsVisible(this View view) => view.Visibility == ViewStates.Visible;

        public static void SetIsVisible(this View view, bool isVisible) => view.Visibility = isVisible ? ViewStates.Visible : ViewStates.Invisible;

        public static double GetOpacity(this View view) => view.Alpha;

        public static void SetOpacity(this View view, double opacity) => view.Alpha = (float)opacity;

        public static void SetIsEnabled(this View view, bool enabled) => view.Enabled = enabled;

        public static void Arrange(this View view, RectF bounds)
        {
            throw new NotImplementedException();

            //if (bounds == new Rect(0, 0, 0, 0))
            //{
            //    view.SetIsVisible(false);
            //}
            //else
            //{
            //    view.SetIsVisible(true);
            //    view.Frame = bounds;
            //}
        }

        public static void SetBackgroundColor(this View view, Color color) => view.SetBackgroundColor(color);

        public static void SetBorderColor(this View view, Color color)
        {
            throw new NotImplementedException();

            //view.BorderColor = color;
        }

        public static void SetBorderColor(this RectangleView view, Color color)
        {
            throw new NotImplementedException();

            //view.BorderColor = color;
        }

        //public static void SetBorderColor(this DrawActionButton view, UIColor color) => view.BorderColor = color;

        //public static void SetBorderColor(this DrawActionToggleButton view, UIColor color) => view.BorderColor = color;

        public static void SetBorderWidth(this RectangleView view, double width)
        {
            throw new NotImplementedException();

            //view.BorderWidth = width;
        }

        public static void SetTextColor(this TextView label, Color color) => label.SetTextColor(color);

        public static void RemoveChild(this ViewGroup parent, View child) => parent.RemoveView(child);

        public static IEnumerable<View> GetChildren(this ViewGroup parent)
        {
            var children = new List<View>();
            for (var i = 0; i < parent.ChildCount; i++)
            {
                children.Add(parent.GetChildAt(i));
            }

            return children;
        }

        public static void Measure(this View view, SizeF availableSize) => view.Measure((int)Math.Round(availableSize.Width), (int)Math.Round(availableSize.Height));
    }
}
