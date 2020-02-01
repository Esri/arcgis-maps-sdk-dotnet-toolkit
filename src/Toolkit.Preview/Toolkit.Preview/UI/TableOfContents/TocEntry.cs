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

#if !__IOS__ && !__ANDROID__ && !NETSTANDARD2_0
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
#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    internal class TocEntryCollection : ObservableCollection<TocEntry>
    {
        private IEnumerable<ILayerContent> _sublayerContents;
        private Action<Action> _uithreadDelegate;
        public TocEntryCollection(IEnumerable<ILayerContent> sublayerContents, bool showLegend, Action<Action> uithreadDelegate = null)
            : base(sublayerContents.Select(s => new TocEntry(s, showLegend)))
        {
            _uithreadDelegate = uithreadDelegate;
            _sublayerContents = sublayerContents;

            if (sublayerContents is INotifyCollectionChanged incc)
            {
                //var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                //{
                //    OnEventAction = (instance, source, eventArgs) => Layers_CollectionChanged(source, eventArgs),
                //    OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent
                //};
                //incc.CollectionChanged += listener.OnEvent;
            }
        }
    }

    /// <summary>
    /// Class used to represent an entry in the Legend control
    /// </summary>
    /// <remarks>
    /// The <see cref="Content"/> property will contain the actual object it represents, mainly <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.
    /// </remarks>

#if NETFX_CORE
    [Windows.UI.Xaml.Data.Bindable]
#endif
    public class TocEntry : INotifyPropertyChanged
    {
        private System.Threading.Tasks.Task<IReadOnlyList<LegendInfo>> _legendInfoLoadTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="TocEntry"/> class.
        /// </summary>
        /// <param name="content">The object this entry represents, usually a <see cref="Layer"/>, <see cref="ILayerContent"/> or <see cref="LegendInfo"/>.</param>
        public TocEntry(object content, bool showLegend)
        {
            Content = content;
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

        private void SetChildren()
        {
            Children = new TocEntryCollection((Content as ILayerContent).SublayerContents, _showLegend);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
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
                    if (value)
                    {
                        if (_legendInfoLoadTask == null)
                        {
                            LoadLegend();
                            return;
                        }
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LegendInfos)));
                }
            }
        }

        private async void LoadLegend()
        {
            if (Content is ILayerContent lc)
            {
                _legendInfoLoadTask = lc.GetLegendInfosAsync();
                try
                {
                    var result = await _legendInfoLoadTask;
                    if (result.Count > 0)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LegendInfos)));
                    }
                }
                catch
                {
                }
            }
        }

        public IEnumerable<TocEntry> Children { get; private set; }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (Content != null)
            {
                return Content.GetHashCode();
            }

            return base.GetHashCode();
        }

        public IEnumerable<LegendInfo> LegendInfos
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
        public override bool Equals(object obj) => obj is TocEntry le && ReferenceEquals(Content, le.Content);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
#endif