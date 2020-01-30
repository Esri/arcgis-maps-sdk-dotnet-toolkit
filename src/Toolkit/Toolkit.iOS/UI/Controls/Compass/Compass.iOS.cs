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
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("Compass")]
    [Category("ArcGIS Runtime Controls")]
    public partial class Compass : IComponent
    {
#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Compass(IntPtr handle)
            : base(handle)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="Compass"/> class with the specified frame.
        /// </summary>
        /// <param name="frame">Frame used by the view, expressed in iOS points.</param>
        public Compass(CGRect frame)
            : base(frame)
        {
            Initialize();
        }

        /// <inheritdoc />
        public override void AwakeFromNib()
        {
            var component = (IComponent)this;
            DesignTime.IsDesignMode = component.Site != null && component.Site.DesignMode;

            Initialize();

            base.AwakeFromNib();
        }

        private void Initialize()
        {
            BackgroundColor = UIColor.Clear;

            if (Heading == 0 && AutoHide && !DesignTime.IsDesignMode)
            {
                Alpha = 0;
            }

            AddGestureRecognizer(new UITapGestureRecognizer(OnTapped));

            InvalidateIntrinsicContentSize();
        }

        private void OnTapped()
        {
            ResetRotation();
        }

        /// <inheritdoc />
        public override void Draw(CGRect rect)
        {
            base.Draw(rect);
            nfloat size = (rect.Width > rect.Height ? rect.Height : rect.Width) - 2;
            double c = size * .5;
            var l = (rect.Width - size) * .5;
            var t = (rect.Height - size) * .5;
            CGRect r = new CGRect(l, t, size, size);
            var context = UIGraphics.GetCurrentContext();
            context.TranslateCTM(rect.Width / 2, rect.Height / 2);
            context.RotateCTM((nfloat)(-Heading / 180 * Math.PI));
            context.TranslateCTM(-rect.Width / 2, -rect.Height / 2);
            context.SetFillColor(UIColor.FromRGB(51, 51, 51).CGColor);
            context.FillEllipseInRect(r);
            context.SetStrokeColor(UIColor.White.CGColor);
            context.StrokeEllipseInRect(r);

            var path = new CGPath();

            // create geometry
            path.AddLines(new CGPoint[]
            {
                        new CGPoint(c - (size * .14) + l, c + t),
                        new CGPoint(c + (size * .14) + l, c + t),
                        new CGPoint(c + l, c - (size * .34) + t),
            });
            path.CloseSubpath();

            // add geometry to graphics context and draw it
            context.SetFillColor(UIColor.FromRGB(199, 85, 46).CGColor);
            context.AddPath(path);
            context.DrawPath(CGPathDrawingMode.Fill);

            path = new CGPath();

            // create geometry
            path.AddLines(new CGPoint[]
            {
                        new CGPoint(c - (size * .14) + l, c + t),
                        new CGPoint(c + (size * .14) + l, c + t),
                        new CGPoint(c + l, c + (size * .34) + t),
            });
            path.CloseSubpath();

            // add geometry to graphics context and draw it
            context.SetFillColor(UIColor.White.CGColor);
            context.AddPath(path);
            context.DrawPath(CGPathDrawingMode.Fill);
        }

        private void UpdateCompassRotation(bool transition)
        {
            if (AutoHide)
            {
                SetVisibility(Heading != 0);
            }

            SetNeedsDisplay();
        }

        private void SetVisibility(bool isVisible)
        {
            nfloat alpha = new nfloat(isVisible || DesignTime.IsDesignMode ? 1.0 : 0.0);
            if (alpha != Alpha)
            {
                UIView.Animate(0.25, () => Alpha = alpha);
            }
        }

        /// <inheritdoc />
        ISite IComponent.Site { get; set; }

        private EventHandler _disposed;

        /// <inheritdoc />
        event EventHandler IComponent.Disposed
        {
            add { _disposed += value; }
            remove { _disposed -= value; }
        }
    }
}