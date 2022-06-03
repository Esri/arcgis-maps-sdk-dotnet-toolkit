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
using Esri.ArcGISRuntime.Mapping.Popups;
using UIKit;

#if NET6_0_OR_GREATER
using nfloat = System.Runtime.InteropServices.NFloat;
#else
using nfloat = System.nfloat;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class DetailsItemCell : UITableViewCell
    {
        private readonly UILabel _label;
        private readonly UILabel _formattedValue;

        public DetailsItemCell(IntPtr handle)
            : base(handle)
        {
            ClipsToBounds = true;
            MultipleTouchEnabled = true;
            SelectionStyle = UITableViewCellSelectionStyle.None;
            IndentationWidth = 10;
            TranslatesAutoresizingMaskIntoConstraints = false;

            _label = new UILabel()
            {
                Font = UIFont.PreferredBody,
                TextColor = UIColor.Gray,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.TopLeft,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap,
            };

            _formattedValue = new UILabel()
            {
                Font = UIFont.PreferredCaption1,
                TextColor = UIColor.Black,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.TopLeft,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap,
            };

            ContentView.AddSubviews(_label, _formattedValue);
        }

        internal void Update(PopupFieldValue field)
        {
            _label.Text = field?.Field?.Label;
            _formattedValue.Text = field?.FormattedValue;
        }

        internal void SetForegroundColor(UIColor foregroundColor)
        {
            nfloat r, g, b, a;
            foregroundColor.GetRGBA(out r, out g, out b, out a);
            _label.TextColor = UIColor.FromRGBA(r, g, b, a / 2);
            _formattedValue.TextColor = foregroundColor;
        }

        private bool _constraintsUpdated = false;

        /// <inheritdoc />
        public override void UpdateConstraints()
        {
            base.UpdateConstraints();

            _label.SetContentHuggingPriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Horizontal);
            _formattedValue.SetContentHuggingPriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Horizontal);
            if (_constraintsUpdated)
            {
                return;
            }

            _constraintsUpdated = true;
            var margin = ContentView.LayoutMarginsGuide;

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _label.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor),
                _label.TopAnchor.ConstraintEqualTo(margin.TopAnchor),
                _label.TrailingAnchor.ConstraintEqualTo(margin.TrailingAnchor),

                _formattedValue.TopAnchor.ConstraintEqualTo(_label.BottomAnchor),
                _formattedValue.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor),
                _formattedValue.TrailingAnchor.ConstraintEqualTo(margin.TrailingAnchor),

                ContentView.BottomAnchor.ConstraintEqualTo(_formattedValue.BottomAnchor),
            });
        }
    }
}
