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
using Esri.ArcGISRuntime.UI;
using System;
using System.ComponentModel;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{

    [DisplayName("SymbolDisplay")]
    [Category("ArcGIS Runtime Controls")]
    public partial class SymbolDisplay : IComponent
    {
        private UIStackView _rootStackView;
        private UIImageView _imageView;

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SymbolDisplay(IntPtr handle) : base(handle)
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

            // At run-time, don't display the sub-views until their dimensions have been calculated
            if (!DesignTime.IsDesignMode)
                Hidden = true;

            _rootStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            _imageView = new UIImageView()
            {
                ClipsToBounds= true,
                Frame = new CGRect(new CGPoint(0,0), new CGSize(40,40)),
                ContentMode = UIViewContentMode.ScaleAspectFit
            };
            _rootStackView.AddSubview(_imageView);

            AddSubview(_rootStackView);

            // Anchor the root stack view to the bottom left of the view
            _rootStackView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _rootStackView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;

            InvalidateIntrinsicContentSize();
        }

        private async void Refresh()
        {
            if (_imageView == null)
            {
                return;
            }

            if (Symbol == null)
            {
                _imageView.Image = null;
                _imageView.Frame = new CGRect(_imageView.Bounds.Location, new CGSize(0, 0));
                return;
            }

#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
            try
            {
                var scale = GetScaleFactor();
                var imageData = await Symbol.CreateSwatchAsync(scale * 96);
                var width = (int)(imageData.Width / scale);
                var height = (int)(imageData.Height / scale);
                _imageView.Frame = new CGRect(_imageView.Bounds.Location, new CGSize(Math.Min(width, 40), Math.Min(height, 40)));
                _imageView.Image = await imageData.ToImageSourceAsync();
            }
            catch
            {
                _imageView.Image = null;
            }
#pragma warning restore ESRI1800
        }

        private static double GetScaleFactor()
        {
            return UIScreen.MainScreen.Scale;
        }

        private bool _isSizeValid = false;

        /// <inheritdoc />
        public override void InvalidateIntrinsicContentSize()
        {
            _isSizeValid = false;
            base.InvalidateIntrinsicContentSize();
        }

        private CGSize _intrinsicContentSize;
        /// <inheritdoc />
        public override CGSize IntrinsicContentSize
        {
            get
            {
                if (!_isSizeValid)
                {
                    _isSizeValid = true;
                    _intrinsicContentSize = MeasureSize();
                }
                return _intrinsicContentSize;
            }
        }

        /// <inheritdoc />
        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
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

        private void SetVisibility(bool isVisible)
        {
            Hidden = !isVisible;
        }

        /// <summary>
        /// Aggregates the size of the view's sub-views
        /// </summary>
        /// <returns>The size of the view</returns>
        private CGSize MeasureSize()
        {
            var totalHeight = 0d;
            var totalWidth = 0d;
            foreach (var row in _rootStackView.ArrangedSubviews)
            {
                var rowWidth = 0d;
                var rowHeight = 0d;
                foreach (var view in ((UIStackView)row).ArrangedSubviews)
                {
                    var elementSize = view.IntrinsicContentSize;
                    if (elementSize.Height > rowHeight)
                    {
                        rowHeight = elementSize.Height;
                    }
                    rowWidth += elementSize.Width;
                }
                if (rowWidth > totalWidth)
                {
                    totalWidth = rowWidth;
                }
                totalHeight += rowHeight;
            }

            return new CGSize(totalWidth, totalHeight);
        }

    }
}