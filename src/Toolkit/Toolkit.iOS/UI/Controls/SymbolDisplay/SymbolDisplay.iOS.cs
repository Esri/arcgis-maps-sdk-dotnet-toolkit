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
using System.Threading.Tasks;
using CoreGraphics;
using Esri.ArcGISRuntime.UI;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("SymbolDisplay")]
    [Category("ArcGIS Runtime Controls")]
    public partial class SymbolDisplay : IComponent
    {
        private UIStackView _rootStackView;
        private UIImageView _imageView;
        internal static readonly nfloat MaxSize = 40;

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SymbolDisplay(IntPtr handle)
            : base(handle)
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
            {
                Hidden = true;
            }

            _rootStackView = new UIStackView()
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Fill,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0,
            };

            _imageView = new UIImageView()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AutosizesSubviews = true,
                AutoresizingMask = UIViewAutoresizing.All,
                ClipsToBounds = true,
                ContentMode = UIViewContentMode.ScaleAspectFit,
            };

            _rootStackView.AddArrangedSubview(_imageView);

            AddSubview(_rootStackView);

            _rootStackView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _rootStackView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;

            _imageView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _imageView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;

            InvalidateIntrinsicContentSize();
        }

        /// <inheritdoc />
        public override void UpdateConstraints()
        {
            base.UpdateConstraints();
            if (!_intrinsicContentSize.IsEmpty)
            {
                _imageView.WidthAnchor.ConstraintEqualTo(_intrinsicContentSize.Height).Active = true;
                _imageView.HeightAnchor.ConstraintEqualTo(_intrinsicContentSize.Height).Active = true;
            }
        }

        private CGSize _intrinsicContentSize = CGSize.Empty;

        /// <inheritdoc />
        public override CGSize IntrinsicContentSize => _intrinsicContentSize;

        /// <inheritdoc />
        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
        }

        /// <inheritdoc />
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

        private async Task UpdateSwatchAsync()
        {
            if (_imageView == null)
            {
                return;
            }

            if (Symbol == null)
            {
                _imageView.Image = null;
                return;
            }

#pragma warning disable ESRI1800 // Add ConfigureAwait(false) - This is UI Dependent code and must return to UI Thread
            try
            {
                var scale = GetScaleFactor();
                var imageData = await Symbol.CreateSwatchAsync(scale * 96);
                var width = (int)(imageData.Width / scale);
                var height = (int)(imageData.Height / scale);
                _imageView.Image = await imageData.ToImageSourceAsync();
                _intrinsicContentSize = new CGSize(Math.Min(width, MaxSize), Math.Min(height, MaxSize));
                Hidden = false;
                InvalidateIntrinsicContentSize();
                SetNeedsUpdateConstraints();
                UpdateConstraints();
                SourceUpdated?.Invoke(this, EventArgs.Empty);
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
    }
}