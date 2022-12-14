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

#if WPF
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI
{
    /// <summary>
    /// Class used to represent an entry in the Legend control.
    /// </summary>
    /// <remarks>
    /// The <see cref="Content"/> property will contain the actual object it represents, mainly <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.
    /// </remarks>
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    public class TocItem : INotifyPropertyChanged, Toolkit.UI.ILayerContentItem
    {
        private System.Threading.Tasks.Task<IList<TocItem>>? _legendInfoLoadTask;
        private bool _isExpanded;
        private WeakReference<TocItem?> _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="TocItem"/> class.
        /// </summary>
        /// <param name="content">The object this entry represents, usually a <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.</param>
        /// <param name="showLegend">Whether the legend should be shown or not.</param>
        /// <param name="depth">The depth of this item in the tree.</param>
        /// <param name="parent">The parent to this item in the tree.</param>
        internal TocItem(object content, bool showLegend, int depth, TocItem? parent)
        {
            Content = content;
            Depth = depth;
            _parent = new WeakReference<TocItem?>(parent);
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

            if (content is INotifyPropertyChanged inpc)
            {
                var listener = new Internal.WeakEventListener<TocItem, INotifyPropertyChanged, object?, PropertyChangedEventArgs>(this, inpc)
                {
                    OnEventAction = static (instance, source, eventArgs) => instance.ContentPropertyChanged(eventArgs.PropertyName),
                    OnDetachAction = static (instance, source, weakEventListener) => source.PropertyChanged -= weakEventListener.OnEvent,
                };
                inpc.PropertyChanged += listener.OnEvent;
            }
        }

        private void ContentPropertyChanged(string? propertyName)
        {
            if (Content is FeatureLayer && propertyName == nameof(FeatureLayer.Renderer))
            {
                RefreshLegend();
            }
        }

        /// <summary>
        /// Gets a reference to the parent of this tree item node.
        /// </summary>
        public TocItem? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out TocItem? parent) == true)
                {
                    return parent;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="ILayerContent"/> that this <see cref="TocItem"/> belongs to.
        /// </summary>
        /// <seealso cref="Layer"/>
        /// <seealso cref="Content"/>
        public ILayerContent? LayerContent
        {
            get
            {
                var tocItem = this;
                while (tocItem != null && !(tocItem.Content is ILayerContent))
                {
                    tocItem = Parent;
                }

                return tocItem?.Content as ILayerContent;
            }
        }

        /// <summary>
        /// Gets the <see cref="Layer"/> that this <see cref="TocItem"/> belongs to.
        /// </summary>
        /// <seealso cref="LayerContent"/>
        /// <seealso cref="Content"/>
        public Layer? Layer
        {
            get
            {
                var tocItem = this;
                while (tocItem != null && !(tocItem.Content is Layer))
                {
                    tocItem = tocItem.Parent;
                }

                return tocItem?.Content as Layer;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded in the TreeView or not.
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
        /// Gets the depth of this item in the TreeView.
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
                        var listener = new Internal.WeakEventListener<TocItem, INotifyCollectionChanged, object?, NotifyCollectionChangedEventArgs>(this, incc)
                        {
                            OnEventAction = static (instance, source, eventArgs) => instance.RefreshChildren(),
                            OnDetachAction = static (instance, source, weakEventListener) => source.CollectionChanged -= weakEventListener.OnEvent,
                        };
                        incc.CollectionChanged += listener.OnEvent;
                    }
                }
            }
        }

        private void RefreshChildren()
        {
            if (Content is ILayerContent ilc && ilc.SublayerContents != null)
            {
                var currentChildren = _children;
                var selector = ilc.SublayerContents.Select(s => currentChildren?.Where(t => t.Content == s).FirstOrDefault() ?? new TocItem(s, _showLegend, Depth + 1, this));
                if (ilc is FeatureCollectionLayer || ilc is GroupLayer)
                {
                    selector = selector.Reverse();
                }

                _children = new List<TocItem>(selector);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
            }
        }

        /// <summary>
        /// Gets the content that this entry represents, usually a <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.
        /// </summary>
        /// <seealso cref="LayerContent"/>
        /// <seealso cref="Layer"/>
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

        /// <summary>
        /// Forces regeneration of the legend for this item.
        /// </summary>
        public void RefreshLegend()
        {
            _legendInfoLoadTask = null;
            if (ShowLegend)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
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
            var task = lc.GetLegendInfosAsync();
            if (task.IsCompleted)
            {
                // If the legend creation completes syncronously, add a small delay to give the UI a chance to react
                // Otherwise the legend will often not update the first time
                await System.Threading.Tasks.Task.Yield();
            }

            var infos = await task;
            return new List<TocItem>(infos.Select(t => new TocItem(t, _showLegend, Depth + 1, this)));
        }

        private IEnumerable<TocItem>? _children;

        /// <summary>
        /// Gets the child entries for this TOC Entry.
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

                if (_showLegend)
                {
                    var infos = LegendInfos;
                    if (infos != null)
                    {
                        foreach (var item in infos)
                        {
                            yield return item;
                        }
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
        /// Gets the legend infos for this entry.
        /// </summary>
        private IEnumerable<TocItem>? LegendInfos
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
        public override bool Equals(object? obj) => obj is TocItem le && ReferenceEquals(Content, le.Content);

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
#endif