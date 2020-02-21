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

#if !__IOS__ && !__ANDROID__ && !NETSTANDARD2_0 && !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.Preview.UI
{
    /// <summary>
    /// Class used to represent an entry in the Legend control
    /// </summary>
    /// <remarks>
    /// The <see cref="Content"/> property will contain the actual object it represents, mainly <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.
    /// </remarks>
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    public class TocItem : INotifyPropertyChanged, Toolkit.UI.ILayerContentItem
    {
        private System.Threading.Tasks.Task<IList<TocItem>> _legendInfoLoadTask;
        private bool _isExpanded;

        /// <summary>
        /// Initializes a new instance of the <see cref="TocItem"/> class.
        /// </summary>
        /// <param name="content">The object this entry represents, usually a <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.</param>
        /// <param name="showLegend">Whether the legend should be shown or not</param>
        /// <param name="depth">The depth of this item in the tree.</param>
        internal TocItem(object content, bool showLegend, int depth)
        {
            Content = content;
            Depth = depth;
            _showLegend = showLegend;
            if (content is ILayerContent)
            {
                if (content is ILoadable loadable && loadable.LoadStatus != LoadStatus.Loaded)
                {
                    loadable.Loaded += (s, e) => SetChildren();
                }
                else
                {
                    SetChildren();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded in the TreeView or not
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        /// <summary>
        /// Gets the depth of this item in the TreeView
        /// </summary>
        public int Depth { get; }

        private void SetChildren()
        {
            if (Content is ILayerContent ilc)
            {
                if (ilc.SublayerContents != null)
                {
                    RefreshChildren();
                    if (ilc.SublayerContents is INotifyCollectionChanged incc)
                    {
                        var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                        {
                            OnEventAction = (instance, source, eventArgs) => RefreshChildren(),
                            OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
                        };
                        incc.CollectionChanged += listener.OnEvent;
                    }

                    if (ilc.SublayerContents.Count > 0)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
                    }
                }
            }
        }

        private void RefreshChildren()
        {
            if (Content is ILayerContent ilc && ilc.SublayerContents != null)
            {
                var selector = ilc.SublayerContents.Select(s => new TocItem(s, _showLegend, Depth + 1));
                if (ilc is FeatureCollectionLayer || ilc is GroupLayer)
                {
                    selector = selector.Reverse();
                }

                _children = new List<TocItem>(selector);
            }
        }

        /// <summary>
        /// Gets the content that this entry represents, usually a <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.
        /// </summary>
        public object Content { get; }

        private bool _showLegend;

        internal bool ShowLegend
        {
            get => _showLegend;
            set
            {
                if (_showLegend != value)
                {
                    _showLegend = value;
                    if (_children != null)
                    {
                        foreach (var item in _children)
                        {
                            item.ShowLegend = value;
                        }
                    }

                    if (value)
                    {
                        if (_legendInfoLoadTask == null)
                        {
                            LoadLegend();
                            return;
                        }
                    }

                    if (_legendInfoLoadTask != null && _legendInfoLoadTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
                    }
                }
            }
        }

        private async void LoadLegend()
        {
            if (Content is ILayerContent lc)
            {
                _legendInfoLoadTask = GetLegendInfosAsync(lc);
                try
                {
                    var result = await _legendInfoLoadTask;
                    if (result.Count > 0)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
                    }
                }
                catch
                {
                }
            }
        }

        private async System.Threading.Tasks.Task<IList<TocItem>> GetLegendInfosAsync(ILayerContent lc)
        {
            var infos = await lc.GetLegendInfosAsync();
            return new List<TocItem>(infos.Select(t => new TocItem(t, _showLegend, Depth + 1)));
        }

        private IEnumerable<TocItem> _children;

        /// <summary>
        /// Gets the child entries for this TOC Entry
        /// </summary>
        public IEnumerable<TocItem> Children
        {
            get
            {
                if (_children != null)
                {
                    foreach (var item in _children)
                    {
                        yield return item;
                    }
                }

                if (_showLegend && LegendInfos != null)
                {
                    foreach (var item in LegendInfos)
                    {
                        yield return item;
                    }
                }
            }
        }


        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (Content != null)
            {
                return Content.GetHashCode();
            }

            return base.GetHashCode();
        }

        /// <summary>
        /// Gets the legend infos for this entry
        /// </summary>
        private IEnumerable<TocItem> LegendInfos
        {
            get
            {
                if (_showLegend)
                {
                    if (_legendInfoLoadTask == null)
                    {
                        LoadLegend();
                    }
                    else if (_legendInfoLoadTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    {
                        return _legendInfoLoadTask.Result;
                    }
                }

                return null;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is TocItem le && ReferenceEquals(Content, le.Content);

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
#endif