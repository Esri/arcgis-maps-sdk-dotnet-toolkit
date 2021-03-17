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
    /// Draws a rectangle on the screen.
    /// </summary>
    /// <remarks>Provides a convenient mechanism for rendering rectangle elements of a certain size.
    /// The specified width and height will be applied to the view's intrinsic content size.</remarks>
    internal class RectangleView : UIView, INotifyPropertyChanged
    {
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

        public RectangleView(double width, double height)
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

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            CGRect renderTarget = new CGRect(rect.Location, _size);
            CGContext ctx = UIGraphics.GetCurrentContext();

            ctx.SetFillColor(BackgroundColor.CGColor);
            ctx.SetStrokeColor(BorderColor.CGColor);
            ctx.FillRect(renderTarget);
            ctx.SetLineWidth((nfloat)BorderWidth);
            ctx.StrokeRect(renderTarget);
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
