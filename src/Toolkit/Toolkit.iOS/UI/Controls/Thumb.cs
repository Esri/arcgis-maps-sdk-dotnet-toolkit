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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Represents a control that can be dragged by the user.
    /// </summary>
    internal class Thumb : UIControl, INotifyPropertyChanged
    {
        public Thumb()
        {
            base.BackgroundColor = UIColor.Clear;
        }

        public Thumb(double width, double height)
            : this()
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets or sets the rectangle's width.
        /// </summary>
        public double Width
        {
            get => _size.Width;
            set
            {
                _size.Width = (nfloat)value;
                InvalidateIntrinsicContentSize();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the rectangle's height.
        /// </summary>
        public double Height
        {
            get => _size.Height;
            set
            {
                _size.Height = (nfloat)value;
                InvalidateIntrinsicContentSize();
                OnPropertyChanged();
            }
        }

        private UIColor _backgroundColor = UIColor.White;

        public override UIColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                SetNeedsDisplay();
            }
        }

        private double _borderWidth = 0.1;

        public double BorderWidth
        {
            get => _borderWidth;
            set
            {
                _borderWidth = value;
                SetNeedsDisplay();
            }
        }

        private CGSize _disabledSize;

        public CGSize DisabledSize
        {
            get => _disabledSize;
            set
            {
                _disabledSize = value;
                SetNeedsDisplay();
            }
        }

        private UIColor _borderColor = UIColor.FromRGBA(80, 80, 80, 255);

        public UIColor BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                SetNeedsDisplay();
            }
        }

        private UIColor _disabledColor = UIColor.Black;

        public UIColor DisabledColor
        {
            get => _disabledColor;
            set
            {
                _disabledColor = value;
                SetNeedsDisplay();
            }
        }

        private double _cornerRadius;

        public double CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = value;
                SetNeedsDisplay();
            }
        }

        private bool _useShadow;

        public bool UseShadow
        {
            get => _useShadow;
            set
            {
                _useShadow = value;
                SetNeedsDisplay();
            }
        }

        public bool IsFocused { get; set; }

        private bool _enabled = true;

        public override bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                SetNeedsDisplay();
            }
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            CGContext ctx = UIGraphics.GetCurrentContext();
            if (Enabled)
            {
                CGRect renderTarget = rect.Inset(4, 4);

                if (UseShadow)
                {
                    var shadowColor = BorderColor.ColorWithAlpha((nfloat)0.4);
                    ctx.SetShadow(offset: new CGSize(width: 0.0, height: 3.0), blur: (nfloat)4.0, color: shadowColor.CGColor);
                }

                ctx.SetFillColor(BackgroundColor.CGColor);
                ctx.SetStrokeColor(BorderColor.CGColor);

                var path = CornerRadius > 0 ? UIBezierPath.FromRoundedRect(renderTarget, (nfloat)CornerRadius) : UIBezierPath.FromRect(renderTarget);
                ctx.AddPath(path.CGPath);
                ctx.FillPath();
                ctx.SetLineWidth((nfloat)BorderWidth);
                ctx.AddPath(path.CGPath);
                ctx.StrokePath();
            }
            else
            {
                var left = rect.Left + 1 + Math.Max((Width - DisabledSize.Width) / 2d, 0);
                var top = rect.Top + Math.Max((Height - DisabledSize.Height) / 2d, 0);
                var width = Math.Min(Math.Min(rect.Width, Width), DisabledSize.Width);
                var height = Math.Min(Math.Min(rect.Height, Height), DisabledSize.Height);

                CGRect disabledRenderTarget = new CGRect(left, top, width, height);
                var disabledPath = UIBezierPath.FromRect(disabledRenderTarget);
                ctx.SetFillColor(DisabledColor.CGColor);
                ctx.FillRect(disabledRenderTarget);
            }
        }

        private CGSize _size = CGSize.Empty;

        public override CGSize IntrinsicContentSize => _size;

        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
