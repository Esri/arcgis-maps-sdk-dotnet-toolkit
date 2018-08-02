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
    [DisplayName("PopupViewer")]
    [Category("ArcGIS Runtime Controls")]
    public partial class PopupViewer : IComponent
    {
        private UIStackView _rootStackView;
        private UILabel _editSummary;
        private UILabel _customHtmlDescription;
        private UITableView _detailsList;

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PopupViewer(IntPtr handle)
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
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Spacing = 0
            };

            _editSummary = new UILabel()
            {
                Font = UIFont.SystemFontOfSize(UIFont.LabelFontSize),
                TextColor = UIColor.Black,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.Center,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap,
                Text = "test"
            };
            _rootStackView.AddArrangedSubview(_editSummary);

            _customHtmlDescription = new UILabel()
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
            _rootStackView.AddArrangedSubview(_customHtmlDescription);

            _detailsList = new UITableView(Bounds)
            {
                ClipsToBounds = true,
                ContentMode = UIViewContentMode.ScaleAspectFill,
                SeparatorStyle = UITableViewCellSeparatorStyle.None,
                AllowsSelection = false,
                Bounces = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AutoresizingMask = UIViewAutoresizing.All,
                RowHeight = UITableView.AutomaticDimension,
                EstimatedRowHeight = SymbolDisplay.MaxSize,
            };
            _detailsList.RegisterClassForCellReuse(typeof(DetailsItemCell), PopupViewerTableSource.CellId);
            _rootStackView.AddArrangedSubview(_detailsList);

            AddSubview(_rootStackView);

            _editSummary.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;

            _customHtmlDescription.TopAnchor.ConstraintEqualTo(_editSummary.BottomAnchor).Active = true;
            _customHtmlDescription.HeightAnchor.ConstraintEqualTo(HeightAnchor).Active = true;
            _customHtmlDescription.WidthAnchor.ConstraintEqualTo(WidthAnchor).Active = true;

            _detailsList.HeightAnchor.ConstraintEqualTo(HeightAnchor).Active = true;
            _detailsList.WidthAnchor.ConstraintEqualTo(WidthAnchor).Active = true;
            _detailsList.TopAnchor.ConstraintEqualTo(_editSummary.BottomAnchor).Active = true;

            InvalidateIntrinsicContentSize();
        }

        /// <inheritdoc />
        public override CGSize IntrinsicContentSize => Bounds.Size;

        /// <inheritdoc />
        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
        }

        /// <inheritdoc cref="IComponent.Site" />
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

        private void Refresh()
        {
            if (_detailsList == null)
            {
                return;
            }

            if (PopupManager == null)
            {
                _detailsList.Source = null;
                _detailsList.ReloadData();
                InvalidateIntrinsicContentSize();
                return;
            }

            _editSummary.Hidden = !PopupManager.ShowEditSummary;
            _editSummary.Text = PopupManager.EditSummary;

            var displayDescription = !string.IsNullOrWhiteSpace(PopupManager.CustomDescriptionHtml);
            _customHtmlDescription.Hidden = !displayDescription;
            _customHtmlDescription.Text = PopupManager.CustomDescriptionHtml?.ToPlainText();

            _detailsList.Hidden = displayDescription;
            if (displayDescription)
            {
                _detailsList.Source = null;
                _detailsList.ReloadData();
            }
            else
            {
                _detailsList.Source = new PopupViewerTableSource(PopupManager.DisplayedFields);
                _detailsList.ReloadData();
            }

            InvalidateIntrinsicContentSize();
            SetNeedsUpdateConstraints();
            UpdateConstraints();
            Hidden = false;
        }
    }
}