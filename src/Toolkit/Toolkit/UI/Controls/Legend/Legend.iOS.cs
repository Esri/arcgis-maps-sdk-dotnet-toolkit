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

#if __IOS__
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [DisplayName("Legend")]
    [Category("ArcGIS Runtime Controls")]
    [Register("Legend")]
    public partial class Legend : UIKit.UIView
    {
        private UITableView _listView;

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
            var source = new LegendTableSource(_datasource);
            _listView.Source = source;
            source.CollectionChanged += Source_CollectionChanged;
            AddSubview(_listView);

            _listView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            _listView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            _listView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
            _listView.HeightAnchor.ConstraintEqualTo(HeightAnchor).Active = true;

            InvalidateIntrinsicContentSize();
        }

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _listView.ReloadData();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                for (int i = e.NewStartingIndex; i < e.NewItems.Count + e.NewStartingIndex; i++)
                {
                    _listView.InsertRows(new NSIndexPath[] { NSIndexPath.FromRowSection(i, 0) }, UITableViewRowAnimation.Automatic);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                for (int i = e.OldStartingIndex; i < e.OldItems.Count + e.OldStartingIndex; i++)
                {
                    _listView.DeleteRows(new NSIndexPath[] { NSIndexPath.FromRowSection(e.OldStartingIndex, 0) }, UITableViewRowAnimation.Automatic);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                _listView.ReloadRows(new NSIndexPath[] { NSIndexPath.Create(Enumerable.Range(e.NewStartingIndex, e.NewItems.Count).ToArray()) }, UITableViewRowAnimation.Automatic);
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                _listView.DeleteRows(new NSIndexPath[] { NSIndexPath.Create(Enumerable.Range(e.OldStartingIndex, e.OldItems.Count).ToArray()) }, UITableViewRowAnimation.None);
                _listView.InsertRows(new NSIndexPath[] { NSIndexPath.Create(Enumerable.Range(e.NewStartingIndex, e.NewItems.Count).ToArray()) }, UITableViewRowAnimation.None);
            }
        }

        private class LegendTableSource : UITableViewSource, INotifyCollectionChanged
        {
            private readonly LegendDataSource _legends;
            internal static readonly NSString CellId = new NSString(nameof(LegendTableViewCell));

            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public LegendTableSource(LegendDataSource legends)
                : base()
            {
                _legends = legends;
                var incc = _legends as INotifyCollectionChanged;
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                {
                    OnEventAction = (instance, source, eventArgs) =>
                    {
                        CollectionChanged?.Invoke(this, eventArgs);
                    },
                    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                };
                incc.CollectionChanged += listener.OnEvent;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return _legends.Count;
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
                    TextColor = UIColorHelper.LabelColor,
                    BackgroundColor = UIColor.Clear,
                    ContentMode = UIViewContentMode.Center,
                    TextAlignment = UITextAlignment.Left,
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };
                _symbol = new SymbolDisplay()
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };

                ContentView.AddSubviews(_symbol, _textLabel);
                _symbol.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 10).Active = true;
                _symbol.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor).Active = true;
                _textLabel.LeadingAnchor.ConstraintEqualTo(_symbol.TrailingAnchor).Active = true;
                _textLabel.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor).Active = true;
            }

            internal void Update(object item)
            {
                if (item is LegendEntry entry)
                {
                    if (entry.Content is Layer layer)
                    {
                        _textLabel.Text = layer.Name;
                        _textLabel.Font = UIFont.SystemFontOfSize(18);
                        _symbol.Symbol = null;
                    }
                    else if (entry.Content is ILayerContent layerContent)
                    {
                        _textLabel.Text = layerContent.Name;
                        _textLabel.Font = UIFont.SystemFontOfSize(14);
                        _symbol.Symbol = null;
                    }
                    else if (entry.Content is LegendInfo legendInfo)
                    {
                        _textLabel.Text = legendInfo.Name;
                        _textLabel.Font = UIFont.SystemFontOfSize(12);
                        _symbol.Symbol = legendInfo.Symbol;
                    }
                }
            }
        }
    }
}
#endif