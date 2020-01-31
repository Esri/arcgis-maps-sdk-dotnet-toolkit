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
using System.Collections.Generic;
using System.ComponentModel;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    internal class LegendItemCell : UITableViewCell
    {
        private readonly UILabel _textLabel;
        private readonly LayerLegend _layerLegend;
        private readonly UITableView _listView;

        public LegendItemCell(IntPtr handle)
            : base(handle)
        {
            SelectionStyle = UITableViewCellSelectionStyle.None;
            TranslatesAutoresizingMaskIntoConstraints = false;

            _textLabel = new UILabel()
            {
                LineBreakMode = UILineBreakMode.TailTruncation,
                Font = UIFont.SystemFontOfSize(UIFont.LabelFontSize),
                TextColor = UIColorHelper.LabelColor,
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.Center,
                TextAlignment = UITextAlignment.Left,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            _layerLegend = new LayerLegend()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                IncludeSublayers = false
            };

            _listView = new UITableView(UIScreen.MainScreen.Bounds)
            {
                ClipsToBounds = true,
                ContentMode = UIViewContentMode.ScaleAspectFill,
                SeparatorStyle = UITableViewCellSeparatorStyle.None,
                AllowsSelection = false,
                Bounces = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AutoresizingMask = UIViewAutoresizing.All,
                RowHeight = UITableView.AutomaticDimension,
                EstimatedRowHeight = 100,
            };
            _listView.RegisterClassForCellReuse(typeof(LegendItemCell), LegendTableSource.CellId);

            ContentView.AddSubviews(_textLabel, _layerLegend, _listView);
        }

        private void Refresh(LayerContentViewModel layerContent, string propertyName)
        {
            if (propertyName == nameof(LayerContentViewModel.Sublayers))
            {
                UpdateSublayers(layerContent);
            }
            else if (propertyName == nameof(LayerContentViewModel.DisplayLegend))
            {
                Hidden = layerContent?.DisplayLegend ?? false;
            }
        }

        private void UpdateSublayers(LayerContentViewModel layerContent)
        {
            var subLayers = layerContent?.Sublayers;
            if (subLayers == null)
            {
                _listView.Source = null;
                _listView.ReloadData();
                InvalidateIntrinsicContentSize();
                return;
            }

            var source = new LegendTableSource(new List<LayerContentViewModel>(subLayers));
            _listView.Source = source;
            source.CollectionChanged += (a, b) => InvokeOnMainThread(() =>
            {
                _listView.ReloadData();
                _listView.SetNeedsUpdateConstraints();
                _listView.UpdateConstraints();
            });
            _listView.ReloadData();
            _listView.SetNeedsUpdateConstraints();
            _listView.UpdateConstraints();
        }

        internal void Update(LayerContentViewModel layerContent)
        {
            if (layerContent == null || !layerContent.DisplayLegend)
            {
                return;
            }

            _textLabel.Font = !layerContent.IsSublayer ?
            UIFont.BoldSystemFontOfSize(UIFont.LabelFontSize)
            : UIFont.SystemFontOfSize(UIFont.LabelFontSize);

            _textLabel.Text = layerContent.LayerContent?.Name;
            _layerLegend.LayerContent = layerContent.LayerContent;
            UpdateSublayers(layerContent);

            if (layerContent is INotifyPropertyChanged)
            {
                var inpc = layerContent as INotifyPropertyChanged;
                var listener = new Internal.WeakEventListener<INotifyPropertyChanged, object, PropertyChangedEventArgs>(inpc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                    {
                        Refresh(instance as LayerContentViewModel, eventArgs.PropertyName);
                    },
                    OnDetachAction = (instance, weakEventListener) => instance.PropertyChanged -= weakEventListener.OnEvent
                };
                inpc.PropertyChanged += listener.OnEvent;
            }

            InvalidateIntrinsicContentSize();
        }

        private bool _constraintsUpdated = false;

        /// <inheritdoc />
        public override void UpdateConstraints()
        {
            base.UpdateConstraints();

            _textLabel.SetContentCompressionResistancePriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Vertical);
            _layerLegend.SetContentCompressionResistancePriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Vertical);
            _listView.SetContentCompressionResistancePriority((float)UILayoutPriority.DefaultHigh, UILayoutConstraintAxis.Vertical);

            if (_constraintsUpdated)
            {
                return;
            }

            _constraintsUpdated = true;
            var margin = ContentView.LayoutMarginsGuide;

            _textLabel.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor).Active = true;
            _layerLegend.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor).Active = true;
            _listView.LeadingAnchor.ConstraintEqualTo(margin.LeadingAnchor).Active = true;

            if (_textLabel.IntrinsicContentSize.Height > 0)
            {
                _textLabel.TopAnchor.ConstraintEqualTo(margin.TopAnchor).Active = true;
            }

            if (_layerLegend.IntrinsicContentSize.Height > 0)
            {
                var bottomAnchor = (_textLabel.IntrinsicContentSize.Height > 0) ? _textLabel.BottomAnchor : margin.BottomAnchor;
                _layerLegend.TopAnchor.ConstraintEqualTo(bottomAnchor).Active = true;
            }

            if (_listView.ContentSize.Height > 0)
            {
                var bottomAnchor = _layerLegend.IntrinsicContentSize.Height > 0 ? _layerLegend.BottomAnchor :
                    (_textLabel.IntrinsicContentSize.Height > 0) ? _textLabel.BottomAnchor : margin.BottomAnchor;
                _listView.TopAnchor.ConstraintEqualTo(bottomAnchor).Active = true;
            }

            var marginBottomAnchor = _listView.ContentSize.Height > 0 ? _listView.BottomAnchor :
                (_layerLegend.IntrinsicContentSize.Height > 0) ? _layerLegend.BottomAnchor :
                (_textLabel.IntrinsicContentSize.Height > 0) ? _textLabel.BottomAnchor :
                margin.BottomAnchor;
            marginBottomAnchor.ConstraintEqualTo(margin.BottomAnchor).Active = true;
        }
    }
}