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
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Toolkit.Internal;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("PopupViewer")]
    [Category("ArcGIS Runtime Controls")]
    public partial class PopupViewer : IComponent
    {
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

            _editSummary = new UILabel()
            {
                Font = UIFont.SystemFontOfSize(UIFont.SystemFontSize),
                ClipsToBounds = true,
                TextColor = UIColorHelper.LabelColor,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.TopLeft,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap,
            };

            _customHtmlDescription = new UILabel()
            {
                Font = UIFont.SystemFontOfSize(UIFont.SystemFontSize),
                ClipsToBounds = true,
                TextColor = UIColorHelper.LabelColor,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.TopLeft,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap,
            };

            _detailsList = new UITableView(CGRect.Empty, UITableViewStyle.Plain)
            {
                PreservesSuperviewLayoutMargins = false,
                SeparatorInset = UIEdgeInsets.Zero,
                LayoutMargins = UIEdgeInsets.Zero,
                BackgroundColor = UIColor.Clear,
                ClipsToBounds = true,
                ContentMode = UIViewContentMode.TopLeft,
                SeparatorStyle = UITableViewCellSeparatorStyle.None,
                AllowsSelection = false,
                Bounces = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                RowHeight = UITableView.AutomaticDimension,
            };
            _detailsList.RegisterClassForCellReuse(typeof(DetailsItemCell), PopupViewerTableSource.CellId);
            AddSubviews(_editSummary, _customHtmlDescription, _detailsList);
            InvalidateIntrinsicContentSize();
        }

        /// <summary>
        /// Gets total size needed for PopupViewer.
        /// </summary>
        /// <returns>The total size of control.</returns>
        private CGSize MeasureSize()
        {
            if (_intrinsicContentSize == CGSize.Empty)
            {
                var height = _editSummary.IntrinsicContentSize.Height + _customHtmlDescription.IntrinsicContentSize.Height + _detailsList.ContentSize.Height + UIFont.LabelFontSize;
                var maxWidth = Bounds.Width;
                if (maxWidth == 0)
                {
                    maxWidth = (nfloat)Math.Max(Math.Max(_editSummary.IntrinsicContentSize.Width, _customHtmlDescription.IntrinsicContentSize.Width), _detailsList.ContentSize.Width);
                }

                _intrinsicContentSize = new CGSize(maxWidth, height);
            }

            return _intrinsicContentSize;
        }

        private CGSize _intrinsicContentSize = CGSize.Empty;

        /// <inheritdoc />
        public override void InvalidateIntrinsicContentSize()
        {
            base.InvalidateIntrinsicContentSize();
            _intrinsicContentSize = CGSize.Empty;
            _constraintsUpdated = false;
        }

        /// <inheritdoc />
        public override CGSize IntrinsicContentSize => MeasureSize();

        /// <inheritdoc />
        public override CGSize SizeThatFits(CGSize size)
        {
            var widthThatFits = Math.Min(size.Width, IntrinsicContentSize.Width);
            var heightThatFits = Math.Min(size.Height, IntrinsicContentSize.Height);
            return new CGSize(widthThatFits, heightThatFits);
        }

        private bool _constraintsUpdated = false;

        /// <inheritdoc />
        public override void UpdateConstraints()
        {
            base.UpdateConstraints();

            if (_constraintsUpdated || PopupManager == null)
            {
                return;
            }

            _constraintsUpdated = true;
            var margin = LayoutMarginsGuide;
            var topAnchor = !string.IsNullOrWhiteSpace(PopupManager.EditSummary) ?
                _editSummary.BottomAnchor : margin.TopAnchor;
            var bottomAnchor = !string.IsNullOrWhiteSpace(PopupManager.CustomDescriptionHtml) ?
                _customHtmlDescription.TopAnchor : _detailsList.TopAnchor;
            if (!string.IsNullOrWhiteSpace(PopupManager.EditSummary))
            {
                _editSummary.TopAnchor.ConstraintEqualTo(margin.TopAnchor).Active = true;
                _editSummary.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor).Active = true;
                _editSummary.LeftAnchor.ConstraintEqualTo(margin.LeftAnchor).Active = true;
                _editSummary.RightAnchor.ConstraintEqualTo(margin.RightAnchor).Active = true;
            }
            else
            {
                _editSummary.Hidden = true;
                _editSummary.Text = null;
            }

            if (!string.IsNullOrWhiteSpace(PopupManager.CustomDescriptionHtml))
            {
                _customHtmlDescription.TopAnchor.ConstraintEqualTo(topAnchor).Active = true;
                _customHtmlDescription.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor).Active = true;
                _customHtmlDescription.LeftAnchor.ConstraintEqualTo(margin.LeftAnchor).Active = true;
                _customHtmlDescription.RightAnchor.ConstraintEqualTo(margin.RightAnchor).Active = true;
            }
            else
            {
                _detailsList.TopAnchor.ConstraintEqualTo(topAnchor).Active = true;
                _detailsList.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor).Active = true;
                _detailsList.LeftAnchor.ConstraintEqualTo(margin.LeftAnchor).Active = true;
                _detailsList.RightAnchor.ConstraintEqualTo(margin.RightAnchor).Active = true;
                _detailsList.BottomAnchor.ConstraintEqualTo(margin.BottomAnchor, -5).Active = true;
            }
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

            if (!string.IsNullOrWhiteSpace(PopupManager.EditSummary))
            {
                _editSummary.Hidden = false;
                _editSummary.Text = PopupManager.EditSummary;
            }
            else
            {
                _editSummary.Hidden = true;
                _editSummary.Text = null;
            }

            if (!string.IsNullOrWhiteSpace(PopupManager.CustomDescriptionHtml))
            {
                _customHtmlDescription.Hidden = false;
                _customHtmlDescription.Text = PopupManager.CustomDescriptionHtml.ToPlainText();
                _detailsList.Hidden = true;
                _detailsList.Source = null;
                _detailsList.ReloadData();
            }
            else
            {
                _customHtmlDescription.Hidden = true;
                _customHtmlDescription.Text = null;
                _detailsList.Hidden = false;
                _detailsList.Source = new PopupViewerTableSource(PopupManager.DisplayedFields, _foregroundColor);
                _detailsList.ReloadData();
            }

            InvalidateIntrinsicContentSize();
            SetNeedsUpdateConstraints();
            UpdateConstraints();
            Hidden = false;
        }

        private UIColor _foregroundColor = UIColorHelper.LabelColor;

        /// <summary>
        /// Gets or sets the color of the foreground elements of the <see cref="PopupViewer"/>.
        /// </summary>
        public UIColor ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;

                _editSummary.TextColor = value;
                _customHtmlDescription.TextColor = value;
                (_detailsList.Source as PopupViewerTableSource)?.SetForegroundColor(value);
            }
        }
    }
}