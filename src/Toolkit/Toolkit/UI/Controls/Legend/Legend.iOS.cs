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
//  *

#if __IOS__
using Esri.ArcGISRuntime.Mapping;
using Foundation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("Legend")]
    [Category("ArcGIS Runtime Controls")]
    [Register("Legend")]
    public partial class Legend : UIKit.UIView
    {
#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// Internal use only.  Invoked by the Xamarin iOS designer.
        /// </summary>
        /// <param name="handle">A platform-specific type that is used to represent a pointer or a handle.</param>
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Legend(IntPtr handle)
            : base(handle)
        {
        }

        /// <inheritdoc />
        public override void AwakeFromNib()
        {
            var component = (IComponent)this;
            DesignTime.IsDesignMode = component.Site != null && component.Site.DesignMode;

            Initialize();

            base.AwakeFromNib();
        }

        private UITableView _listView;

        private void Initialize()
        {
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
            _listView.RegisterClassForCellReuse(typeof(LegendTableViewCell), LegendTableSource.CellId);
            AddSubview(_listView);

            _listView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _listView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            _listView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
            _listView.HeightAnchor.ConstraintEqualTo(HeightAnchor).Active = true;

            InvalidateIntrinsicContentSize();
        }

        private class LegendTableSource : UITableViewSource, INotifyCollectionChanged
        {
            private readonly IReadOnlyList<object> _legends;
            internal static readonly NSString CellId = new NSString(nameof(LegendTableViewCell));

            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public LegendTableSource(IReadOnlyList<object> legends)
                : base()
            {
                _legends = legends;
                if (_legends is INotifyCollectionChanged)
                {
                    var incc = _legends as INotifyCollectionChanged;
                    var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                    {
                        OnEventAction = (instance, source, eventArgs) =>
                        {
                            CollectionChanged?.Invoke(this, eventArgs);
                        },
                        OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
                    };
                    incc.CollectionChanged += listener.OnEvent;
                }
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return _legends?.Count ?? 0;
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var info = _legends[indexPath.Row];
                var cell = tableView.DequeueReusableCell(CellId, indexPath);
                (cell as LegendTableViewCell)?.Update(info);
                cell?.SetNeedsUpdateConstraints();
                cell?.UpdateConstraints();
                return cell;
            }
        }

        private class LegendTableViewCell : UITableViewCell
        {
            private readonly UILabel _textLabel;
            private readonly SymbolDisplay _symbol;

            public LegendTableViewCell(IntPtr handle)
                : base(handle)
            {
                SelectionStyle = UITableViewCellSelectionStyle.None;
                TranslatesAutoresizingMaskIntoConstraints = false;

                _textLabel = new UILabel()
                {
                    LineBreakMode = UILineBreakMode.TailTruncation,
                    Font = UIFont.SystemFontOfSize(UIFont.LabelFontSize),
                    TextColor = UIColor.Black,
                    BackgroundColor = UIColor.Clear,
                    ContentMode = UIViewContentMode.Center,
                    TextAlignment = UITextAlignment.Left,
                    TranslatesAutoresizingMaskIntoConstraints = false
                };
                _symbol = new SymbolDisplay()
                {
                };
                ContentView.AddSubviews(_textLabel /*, _symbol*/);
            }

            internal void Update(object layeritem)
            {
                if (layeritem is Layer layer)
                {
                    _textLabel.Text = layer.Name;
                    // _textLabel.SetTextSize(ComplexUnitType.Dip, 20);
                    // _textLabel.SetTypeface(null, TypefaceStyle.Bold);
                    // _symbol.Visibility = ViewStates.Gone;
                    // _symbol.Symbol = null;
                }
                else if (layeritem is ILayerContent layerContent)
                {
                    _textLabel.Text = layerContent.Name;
                    // _textLabel.SetTextSize(ComplexUnitType.Dip, 18);
                    // _textLabel.SetTypeface(null, TypefaceStyle.Normal);
                    // _symbol.Visibility = ViewStates.Gone;
                    // _symbol.Symbol = null;
                }
                else if (layeritem is LegendInfo legendInfo)
                {
                    _textLabel.Text = legendInfo.Name;
                    // _textLabel.SetTextSize(ComplexUnitType.Dip, 18);
                    // _textLabel.SetTypeface(null, TypefaceStyle.Normal);
                    // _symbol.Visibility = ViewStates.Visible;
                    // _symbol.Symbol = legendInfo.Symbol;
                }
            }
        }
    }
}
#endif