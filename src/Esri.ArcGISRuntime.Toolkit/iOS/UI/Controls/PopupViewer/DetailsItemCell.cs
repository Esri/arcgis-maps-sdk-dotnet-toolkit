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
                Font = UIFont.SystemFontOfSize(UIFont.LabelFontSize),
                TextColor = UIColor.Gray,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.Center,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap
            };

            _formattedValue = new UILabel()
            {
                Font = UIFont.SystemFontOfSize(UIFont.LabelFontSize),
                TextColor = UIColor.Black,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.Center,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap
            };

            ContentView.AddSubviews(_label, _formattedValue);
        }

        public void Update(PopupFieldValue field)
        {
            _label.Text = field?.Field?.Label;
            _formattedValue.Text = field?.FormattedValue;
        }

        private bool _constraintsUpdated = false;

        /// <inheritdoc />
        public override void UpdateConstraints()
        {
            base.UpdateConstraints();
            _label.SetContentCompressionResistancePriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Vertical);
            _formattedValue.SetContentCompressionResistancePriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Vertical);

            _label.SetContentHuggingPriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Horizontal);
            _formattedValue.SetContentHuggingPriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Horizontal);
            if (_constraintsUpdated)
            {
                return;
            }

            _constraintsUpdated = true;
            var margin = ContentView.LayoutMarginsGuide;

            _label.TopAnchor.ConstraintEqualTo(margin.TopAnchor).Active = true;
            _label.LeftAnchor.ConstraintEqualTo(margin.LeftAnchor).Active = true;

            _formattedValue.TopAnchor.ConstraintEqualTo(_label.BottomAnchor).Active = true;
            _formattedValue.LeftAnchor.ConstraintEqualTo(margin.LeftAnchor).Active = true;
        }
    }
}
