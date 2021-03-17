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
using Esri.ArcGISRuntime.Mapping;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LayerLegendItemCell : UITableViewCell
    {
        private readonly SymbolDisplay _symbolDisplay;
        private readonly UILabel _textLabel;

        public LayerLegendItemCell(IntPtr handle)
            : base(handle)
        {
            ClipsToBounds = true;
            MultipleTouchEnabled = true;
            SelectionStyle = UITableViewCellSelectionStyle.None;
            IndentationWidth = 10;
            TranslatesAutoresizingMaskIntoConstraints = false;

            _symbolDisplay = new SymbolDisplay()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
            };

            _textLabel = new UILabel()
            {
                Font = UIFont.SystemFontOfSize(UIFont.LabelFontSize),
                TextColor = UIColorHelper.LabelColor,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.Center,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                LineBreakMode = UILineBreakMode.TailTruncation,
            };

            ContentView.AddSubviews(_symbolDisplay, _textLabel);
        }

        public void Update(LegendInfo info)
        {
            _symbolDisplay.Symbol = info?.Symbol;
            _textLabel.Text = info?.Name;
        }

        private bool _constraintsUpdated = false;

        /// <inheritdoc />
        public override void UpdateConstraints()
        {
            base.UpdateConstraints();

            _symbolDisplay.SetContentCompressionResistancePriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Vertical);

            if (_constraintsUpdated)
            {
                return;
            }

            _constraintsUpdated = true;
            var margin = ContentView.LayoutMarginsGuide;

            _symbolDisplay.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor).Active = true;
            _symbolDisplay.CenterYAnchor.ConstraintEqualTo(margin.CenterYAnchor).Active = true;

            _textLabel.LeadingAnchor.ConstraintEqualTo(_symbolDisplay.TrailingAnchor).Active = true;
            _textLabel.CenterYAnchor.ConstraintEqualTo(margin.CenterYAnchor).Active = true;
        }
    }
}
