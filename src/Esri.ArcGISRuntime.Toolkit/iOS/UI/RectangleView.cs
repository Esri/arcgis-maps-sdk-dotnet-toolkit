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

using CoreGraphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Draws a rectangle on the screen
    /// </summary>
    /// <remarks>Provides a convenient mechanism for rendering rectangle elements of a certain size.
    /// The specified width and height will be applied to the view's intrinsic content size.</remarks>
    internal class RectangleView : UIControl, INotifyPropertyChanged
    {
        private UIView _childView;

        public RectangleView()
        {
            base.BackgroundColor = UIColor.Clear;
        }

        public override CGRect Frame
        { 
            get => base.Frame;
            set
            {
                base.Frame = value;
                OnPropertyChanged();
            }
        }

        public RectangleView(double width, double height) : this()
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets or sets the rectangle's width
        /// </summary>
        public double Width
        {
            get { return _size.Width; }
            set
            {
                _size.Width = (nfloat)value;
                InvalidateIntrinsicContentSize();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the rectangle's height
        /// </summary>
        public double Height
        {
            get { return _size.Height; }
            set
            {
                _size.Height = (nfloat)value;
                InvalidateIntrinsicContentSize();
                OnPropertyChanged();
            }
        }

        private UIColor _backgroundColor = UIColor.Clear;
        public override UIColor BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                SetNeedsDisplay();
            }
        }

        private double _borderWidth;
        public double BorderWidth
        {
            get => _borderWidth;
            set
            {
                _borderWidth = value;
                SetNeedsDisplay();
            }
        }

        private UIColor _borderColor = UIColor.Clear;
        public UIColor BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
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

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            CGRect renderTarget = CornerRadius > 0 ? rect.Inset(2, 2) : new CGRect(rect.Location, _size);
            CGContext ctx = UIGraphics.GetCurrentContext();

            if (UseShadow)
            {
                var shadowColor = BorderColor.ColorWithAlpha((nfloat)0.4);
                ctx.SetShadow(offset: new CGSize(width: 0.0, height: 1.0), blur: (nfloat)1.0, color: shadowColor.CGColor);
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

        private CGSize _size = CGSize.Empty;

        public override CGSize IntrinsicContentSize => _size;

        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
        }

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged" />
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
