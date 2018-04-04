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
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A toggle button for rendering arbitrary content as the button surface.  Different content
    /// may be specified for the button's toggle states.
    /// </summary>
    internal class DrawActionToggleButton : UIButton
    {
        public DrawActionToggleButton()
            : base()
        {
            Layer.BackgroundColor = UIColor.Clear.CGColor;
            Layer.BorderColor = UIColor.Clear.CGColor;

            TouchUpInside += (s, e) => IsChecked = !IsChecked;
        }

        private UIColor _backgroundColor;

        public override UIColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    SetNeedsDisplay();
                }
            }
        }

        private UIColor _borderColor;

        public UIColor BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    SetNeedsDisplay();
                }
            }
        }

        private double _borderWidth;

        public double BorderWidth
        {
            get => _borderWidth;
            set
            {
                if (_borderWidth != value)
                {
                    _borderWidth = value;
                    SetNeedsDisplay();
                }
            }
        }

        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value)
                {
                    return;
                }

                _isChecked = value;
                SetNeedsDisplay();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the action to be executed during the control's rendering pass if the switch is on.  This action is responsible for rendering the control's UI.
        /// </summary>
        /// <value>The on draw action.</value>
        public Action<CGContext, DrawActionToggleButton> DrawCheckedContentAction { get; set; }

        /// <summary>
        /// Gets or sets the action to be executed during the control's rendering pass if the switch is off.  This action is responsible for rendering the control's UI.
        /// </summary>
        /// <value>The on draw action.</value>
        public Action<CGContext, DrawActionToggleButton> DrawUncheckedContentAction { get; set; }

        public override void Draw(CGRect rect)
        {
            if (IsChecked)
            {
                DrawCheckedContentAction?.Invoke(UIGraphics.GetCurrentContext(), this);
            }
            else
            {
                DrawUncheckedContentAction?.Invoke(UIGraphics.GetCurrentContext(), this);
            }
        }

        public event EventHandler CheckedChanged;
    }
}
