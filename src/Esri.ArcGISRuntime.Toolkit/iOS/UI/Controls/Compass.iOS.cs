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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Compass")]
    [DisplayName("Compass")]
    [Category("ArcGIS Runtime Controls")]
    public partial class Compass : IComponent
    {
        private class NorthArrowShape : UIView
        {
            public NorthArrowShape() : base() { }

            public override void Draw(CGRect rect)
            {
                base.Draw(rect);
                using (CGContext g = UIGraphics.GetCurrentContext())
                {
                    //set up drawing attributes
                    g.SetLineWidth(10);
                    UIColor.Blue.SetFill();
                    UIColor.Red.SetStroke();

                    //create geometry
                    var path = new CGPath();

                    path.AddLines(new CGPoint[]{
                        new CGPoint (100, 200),
                        new CGPoint (160, 100),
                        new CGPoint (220, 200)});

                    path.CloseSubpath();

                    //add geometry to graphics context and draw it
                    g.AddPath(path);
                    g.DrawPath(CGPathDrawingMode.FillStroke);
                }
            }
        }

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Compass(IntPtr handle) : base(handle)
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
        NorthArrowShape _arrow;
        private void Initialize()
        {
            BackgroundColor = UIColor.Clear;

            // At run-time, don't display the sub-views until their dimensions have been calculated
            if (!DesignTime.IsDesignMode)
                Hidden = true;

            _arrow = new NorthArrowShape();
            AddSubview(_arrow);

            InvalidateIntrinsicContentSize();
        }
        private void UpdateCompassRotation(bool transition)
        {
            SetVisibility(Heading != 0);
            _arrow.Transform = CGAffineTransform.MakeRotation((float)(Math.PI * Heading / 180d));
        }

        private void SetVisibility(bool isVisible)
        {
            Hidden = !isVisible;
        }

        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        ISite IComponent.Site { get; set; }

        private EventHandler _disposed;

        /// <summary>
        /// Internal use only
        /// </summary>
        event EventHandler IComponent.Disposed
        {
            add { _disposed += value; }
            remove { _disposed -= value; }
        }
    }
}