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

using CoreGraphics;
using Esri.ArcGISRuntime.Toolkit.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal static class ViewExtensions
    {
        public static void SetMargin(this UIView view, double left, double top, double right, double bottom)
        {
            if (view?.Superview?.Frame == null)
                return;
            
            var frameRight = view.Superview.Frame.Width - right;
            var frameBottom = view.Superview.Frame.Height - bottom;
            var width = frameRight - left;
            var height = frameBottom - top;

            if ((nfloat)left != view.Frame.Left)
                view.Frame = new CGRect(left, view.Frame.Top, view.Frame.Width, view.Frame.Height);
        }

        public static double GetActualWidth(this UIView view) => !view.IntrinsicContentSize.IsEmpty ? view.IntrinsicContentSize.Width : view.Bounds.Width;

        public static void SetWidth(this UIView view, double width)
        {

        }

        public static bool GetIsVisible(this UIView view) => !view.Hidden;

        public static void SetIsVisible(this UIView view, bool isVisible) => view.Hidden = !isVisible;

        public static double GetOpacity(this UIView view) => view.Alpha;

        public static void SetOpacity(this UIView view, double opacity) => view.Alpha = (nfloat)opacity;

        public static bool GetIsEnabled(this UIControl control) => control.Enabled;

        public static void SetIsEnabled(this UIControl control, bool enabled) => control.Enabled = enabled;

        public static void SetIsChecked(this UISwitch toggleButton, bool isChecked) => toggleButton.On = isChecked;

        public static void Arrange(this UIView view, CGRect bounds) => view.Frame = bounds;
    }
}
