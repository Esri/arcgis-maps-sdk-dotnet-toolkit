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
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("LayerLegend")]
    [Category("ArcGIS Runtime Controls")]
    public partial class LayerLegend : IComponent
    {
        private UIStackView _rootStackView;
        private UITableView _listView;

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LayerLegend(IntPtr handle) : base(handle)
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

            _listView = new UITableView(UIScreen.MainScreen.Bounds)
            {

                TranslatesAutoresizingMaskIntoConstraints = false,
                AutoresizingMask = UIViewAutoresizing.All,
                RowHeight = UITableView.AutomaticDimension,
                EstimatedRowHeight = 40,
            };
            _listView.RegisterClassForCellReuse(typeof(LayerLegendItemCell), LayerLegendTableSource.CellId);
            _rootStackView.AddSubview(_listView);

            AddSubview(_rootStackView);

            _listView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _listView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            _listView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
            _listView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;

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
        
        private void Refresh()
        {
            if (_listView == null)
            {
                return;
            }

            if (LayerContent == null)
            {
                _listView.Source = null;
                return;
            }

            if (LayerContent is ILoadable)
            {
                if ((LayerContent as ILoadable).LoadStatus != LoadStatus.Loaded)
                {
                    (LayerContent as ILoadable).Loaded += Layer_loaded;
                    (LayerContent as ILoadable).LoadAsync();
                    return;
                }
            }

            var items = new ObservableCollection<LegendInfo>();
            LoadRecursive(items, LayerContent, ShowEntireTreeHierarchy);
            var source = new LayerLegendTableSource(items);
            _listView.Source = source;
            _listView.ReloadData();
            source.CollectionChanged += (a, b) => InvokeOnMainThread(() => _listView.ReloadData());
            Hidden = false;
            InvalidateIntrinsicContentSize();
        }

        private void Layer_loaded(object sender, EventArgs e)
        {
            (sender as ILoadable).Loaded -= Layer_loaded;
            InvokeOnMainThread(() => Refresh());
        }
    }
}